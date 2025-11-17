using AwesomeAssertions;
using EventBookingSystem.Application.DTOs;
using EventBookingSystem.Application.IntegrationTests.Fixtures;
using EventBookingSystem.Application.IntegrationTests.Helpers;
using EventBookingSystem.Application.Models;
using EventBookingSystem.Application.Services;
using EventBookingSystem.Domain.Services;

namespace EventBookingSystem.Application.IntegrationTests.Services
{
    /// <summary>
    /// Integration tests for BookingApplicationService.
    /// Tests the complete workflow from command ? repositories ? database ? response.
    /// </summary>
    [TestClass]
    public class BookingApplicationServiceIntegrationTests
    {
        private DatabaseFixture _database = null!;
        private BookingApplicationService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            // Create fresh database for each test
            _database = new DatabaseFixture();

            // Create domain services
            var reservationService = new EventReservationService();
            var bookingService = new BookingService(reservationService);

            // Create application service with real repositories
            _service = new BookingApplicationService(
                _database.BookingRepository,
                _database.EventRepository,
                _database.UserRepository,
                _database.VenueRepository,
                bookingService);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _database?.Dispose();
        }

        [TestMethod]
        public async Task CreateBookingAsync_GeneralAdmissionEvent_FullWorkflow_CreatesBookingSuccessfully()
        {
            // Arrange - Setup complete test scenario
            
            // 1. Create and save venue
            var venue = IntegrationTestDataBuilder.CreateVenue(id: 1, name: "Concert Hall", sectionCount: 1, seatsPerSection: 1000);
            await _database.VenueRepository.AddAsync(venue);

            // 2. Create and save user
            var user = IntegrationTestDataBuilder.CreateUser(id: 1, name: "John Doe", email: "john@example.com");
            await _database.UserRepository.AddAsync(user);

            // 3. Create and save general admission event
            var evnt = IntegrationTestDataBuilder.CreateGeneralAdmissionEvent(
                1,  // id
                venue.Id,  // venueId
                "Test Concert",  // name
                10,  // capacity
                50m);  // price
            evnt.Venue = venue;
            await _database.EventRepository.AddAsync(evnt);

            // 4. Create booking command
            var command = new CreateBookingCommand
            {
                UserId = user.Id,
                EventId = evnt.Id,
                Quantity = 2
            };

            // Act - Execute the full workflow
            var result = await _service.CreateBookingAsync(command);

            // Assert - Verify complete result
            result.Should().NotBeNull();
            result.IsSuccessful.Should().BeTrue();
            result.BookingId.Should().NotBeNull();
            result.TotalAmount.Should().Be(100m, because: "2 tickets * $50 = $100"); // 2 * 50
            result.Message.Should().Contain("successfully");

            // Verify booking was saved to database
            var savedBooking = await _database.BookingRepository.GetByIdAsync(result.BookingId!.Value);
            savedBooking.Should().NotBeNull();
            savedBooking!.User.Id.Should().Be(user.Id);
            savedBooking.Event.Id.Should().Be(evnt.Id);
            savedBooking.TotalAmount.Should().Be(100m);
            // GA bookings don't have BookingItems - capacity tracked on the event itself
            savedBooking.BookingItems.Should().BeEmpty(because: "GA bookings don't create BookingItems");

            // Verify event capacity was updated
            var updatedEvent = await _database.EventRepository.GetByIdAsync(evnt.Id);
            var gaEvent = updatedEvent as EventBookingSystem.Domain.Entities.GeneralAdmissionEvent;
            gaEvent.Should().NotBeNull();
            gaEvent!.TotalReserved.Should().Be(2);
            gaEvent.AvailableCapacity.Should().Be(8); // 10 - 2
        }

        [TestMethod]
        public async Task CreateBookingAsync_SectionBasedEvent_FullWorkflow_CreatesBookingSuccessfully()
        {
            // Arrange
            var venue = IntegrationTestDataBuilder.CreateVenue(id: 1, name: "Stadium", sectionCount: 3, seatsPerSection: 500);
            await _database.VenueRepository.AddAsync(venue);

            var user = IntegrationTestDataBuilder.CreateUser(id: 2, name: "Jane Smith", email: "jane@example.com");
            await _database.UserRepository.AddAsync(user);

            var evnt = IntegrationTestDataBuilder.CreateSectionBasedEvent(
                1,  // id
                venue.Id,  // venueId
                "Test Section Event",  // name
                (1, 500, 100m));  // sections
            evnt.Venue = venue;
            await _database.EventRepository.AddAsync(evnt);

            var command = new CreateBookingCommand
            {
                UserId = user.Id,
                EventId = evnt.Id,
                Quantity = 4,
                SectionId = 1  // VIP section
            };

            // Act
            var result = await _service.CreateBookingAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessful.Should().BeTrue();
            result.BookingId.Should().NotBeNull();
            result.TotalAmount.Should().Be(400m); // 4 * 100

            // Verify database state
            var savedBooking = await _database.BookingRepository.GetByIdAsync(result.BookingId!.Value);
            savedBooking.Should().NotBeNull();
            savedBooking!.BookingType.Should().Be(EventBookingSystem.Domain.Entities.BookingType.Section);
            savedBooking.BookingItems.Should().HaveCount(1);
            savedBooking.BookingItems.First().Quantity.Should().Be(4);

            // Verify section inventory was updated
            var updatedEvent = await _database.EventRepository.GetByIdAsync(evnt.Id);
            var sbEvent = updatedEvent as EventBookingSystem.Domain.Entities.SectionBasedEvent;
            sbEvent.Should().NotBeNull();
            var vipSection = sbEvent!.GetSection(1);
            vipSection!.Booked.Should().Be(4);
            vipSection.Remaining.Should().Be(496);
        }

        [TestMethod]
        public async Task CreateBookingAsync_UserNotFound_ReturnsFailure()
        {
            // Arrange
            var venue = IntegrationTestDataBuilder.CreateVenue();
            await _database.VenueRepository.AddAsync(venue);

            var evnt = IntegrationTestDataBuilder.CreateGeneralAdmissionEvent(1, venue.Id);
            evnt.Venue = venue;
            await _database.EventRepository.AddAsync(evnt);

            var command = new CreateBookingCommand
            {
                UserId = 999, // Non-existent user
                EventId = evnt.Id,
                Quantity = 1
            };

            // Act
            var result = await _service.CreateBookingAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessful.Should().BeFalse();
            result.Message.Should().Contain("User not found");
            result.BookingId.Should().BeNull();

            // Verify no booking was created in database
            var allBookings = await _database.BookingRepository.GetAllBookings();
            allBookings.Should().BeEmpty();
        }

        [TestMethod]
        public async Task CreateBookingAsync_EventNotFound_ReturnsFailure()
        {
            // Arrange
            var user = IntegrationTestDataBuilder.CreateUser();
            await _database.UserRepository.AddAsync(user);

            var command = new CreateBookingCommand
            {
                UserId = user.Id,
                EventId = 999, // Non-existent event
                Quantity = 1
            };

            // Act
            var result = await _service.CreateBookingAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessful.Should().BeFalse();
            result.Message.Should().Contain("Event not found");

            // Verify no booking was created
            var allBookings = await _database.BookingRepository.GetAllBookings();
            allBookings.Should().BeEmpty();
        }

        [TestMethod]
        public async Task CreateBookingAsync_InsufficientCapacity_ReturnsFailure()
        {
            // Arrange
            var venue = IntegrationTestDataBuilder.CreateVenue();
            await _database.VenueRepository.AddAsync(venue);

            var user = IntegrationTestDataBuilder.CreateUser();
            await _database.UserRepository.AddAsync(user);

            var evnt = IntegrationTestDataBuilder.CreateGeneralAdmissionEvent(
                venueId: venue.Id,
                capacity: 10,  // Small capacity
                price: 50m);
            evnt.Venue = venue;
            evnt.ReserveTickets(8); // Reserve most of capacity
            await _database.EventRepository.AddAsync(evnt);

            var command = new CreateBookingCommand
            {
                UserId = user.Id,
                EventId = evnt.Id,
                Quantity = 5  // More than remaining capacity
            };

            // Act
            var result = await _service.CreateBookingAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessful.Should().BeFalse();
            result.Message.Should().Contain("Insufficient capacity");

            // Verify no booking was created
            var userBookings = await _database.BookingRepository.GetByUserIdAsync(user.Id);
            userBookings.Should().BeEmpty();
        }

        [TestMethod]
        public async Task CreateBookingAsync_SectionBasedWithoutSectionId_ReturnsFailure()
        {
            // Arrange
            var venue = IntegrationTestDataBuilder.CreateVenue();
            await _database.VenueRepository.AddAsync(venue);

            var user = IntegrationTestDataBuilder.CreateUser();
            await _database.UserRepository.AddAsync(user);

            var evnt = IntegrationTestDataBuilder.CreateSectionBasedEvent(1, venue.Id, "Test Game", (1, 500, 100m));
            evnt.Venue = venue;
            await _database.EventRepository.AddAsync(evnt);

            var command = new CreateBookingCommand
            {
                UserId = user.Id,
                EventId = evnt.Id,
                Quantity = 2
                // Missing SectionId!
            };

            // Act
            var result = await _service.CreateBookingAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessful.Should().BeFalse();
            result.Message.Should().Contain("Section ID is required");
        }

        [TestMethod]
        public async Task CreateBookingAsync_MultipleBookings_AllSucceed()
        {
            // Arrange - Test concurrent bookings
            var venue = IntegrationTestDataBuilder.CreateVenue();
            await _database.VenueRepository.AddAsync(venue);

            var user1 = IntegrationTestDataBuilder.CreateUser(id: 1, email: "user1@example.com");
            var user2 = IntegrationTestDataBuilder.CreateUser(id: 2, email: "user2@example.com");
            await _database.UserRepository.AddAsync(user1);
            await _database.UserRepository.AddAsync(user2);

            var evnt = IntegrationTestDataBuilder.CreateGeneralAdmissionEvent(1, venue.Id, "Test Concert", 1000);
            evnt.Venue = venue;
            await _database.EventRepository.AddAsync(evnt);

            var command1 = new CreateBookingCommand { UserId = user1.Id, EventId = evnt.Id, Quantity = 2 };
            var command2 = new CreateBookingCommand { UserId = user2.Id, EventId = evnt.Id, Quantity = 3 };

            // Act - Create multiple bookings
            var result1 = await _service.CreateBookingAsync(command1);
            var result2 = await _service.CreateBookingAsync(command2);

            // Assert - Both should succeed
            result1.IsSuccessful.Should().BeTrue();
            result2.IsSuccessful.Should().BeTrue();

            // Verify database state
            var user1Bookings = await _database.BookingRepository.GetByUserIdAsync(user1.Id);
            var user2Bookings = await _database.BookingRepository.GetByUserIdAsync(user2.Id);
            
            user1Bookings.Should().HaveCount(1);
            user2Bookings.Should().HaveCount(1);

            // Verify total capacity used
            var updatedEvent = await _database.EventRepository.GetByIdAsync(evnt.Id);
            var gaEvent = updatedEvent as EventBookingSystem.Domain.Entities.GeneralAdmissionEvent;
            gaEvent!.TotalReserved.Should().Be(5); // 2 + 3
        }

        [TestMethod]
        public async Task CreateBookingAsync_ReservedSeatingEvent_FullWorkflow_CreatesBookingSuccessfully()
        {
            // Arrange
            var venue = IntegrationTestDataBuilder.CreateVenue();
            await _database.VenueRepository.AddAsync(venue);

            var user = IntegrationTestDataBuilder.CreateUser();
            await _database.UserRepository.AddAsync(user);

            var evnt = IntegrationTestDataBuilder.CreateReservedSeatingEvent(1, venue.Id, "Test Play", 100);
            evnt.Venue = venue;
            await _database.EventRepository.AddAsync(evnt);

            var command = new CreateBookingCommand
            {
                UserId = user.Id,
                EventId = evnt.Id,
                Quantity = 1,
                SeatId = 1  // Reserve specific seat
            };

            // Act
            var result = await _service.CreateBookingAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessful.Should().BeTrue();
            result.BookingId.Should().NotBeNull();

            // Verify seat was reserved
            var savedBooking = await _database.BookingRepository.GetByIdAsync(result.BookingId!.Value);
            savedBooking!.BookingType.Should().Be(EventBookingSystem.Domain.Entities.BookingType.Seat);
            savedBooking.BookingItems.Should().HaveCount(1);

            var updatedEvent = await _database.EventRepository.GetByIdAsync(evnt.Id);
            var rsEvent = updatedEvent as EventBookingSystem.Domain.Entities.ReservedSeatingEvent;
            rsEvent!.GetSeat(1)!.Status.Should().Be(EventBookingSystem.Domain.Entities.SeatStatus.Reserved);
        }
    }
}
