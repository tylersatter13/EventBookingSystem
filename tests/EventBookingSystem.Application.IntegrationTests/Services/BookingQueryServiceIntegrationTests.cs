using AwesomeAssertions;
using EventBookingSystem.Application.IntegrationTests.Fixtures;
using EventBookingSystem.Application.IntegrationTests.Helpers;
using EventBookingSystem.Application.Services;
using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Domain.Services;

namespace EventBookingSystem.Application.IntegrationTests.Services
{
    /// <summary>
    /// Integration tests for BookingQueryService.
    /// Tests the complete workflow from service ? repositories ? database ? response.
    /// </summary>
    [TestClass]
    public class BookingQueryServiceIntegrationTests
    {
        private DatabaseFixture _database = null!;
        private BookingQueryService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _database = new DatabaseFixture();
            _service = new BookingQueryService(_database.BookingRepository, _database.EventRepository);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _database?.Dispose();
        }

        [TestMethod]
        public async Task GetBookingsByUserIdAsync_WithMultipleBookings_ReturnsAllUserBookings()
        {
            // Arrange - Create test data
            var venue = IntegrationTestDataBuilder.CreateVenue(id: 1, name: "Test Venue");
            await _database.VenueRepository.AddAsync(venue);

            var user1 = IntegrationTestDataBuilder.CreateUser(id: 1, name: "User One", email: "user1@example.com");
            var user2 = IntegrationTestDataBuilder.CreateUser(id: 2, name: "User Two", email: "user2@example.com");
            await _database.UserRepository.AddAsync(user1);
            await _database.UserRepository.AddAsync(user2);

            var event1 = IntegrationTestDataBuilder.CreateGeneralAdmissionEvent(1, venue.Id, "Event 1", 100, 50m);
            event1.Venue = venue;
            await _database.EventRepository.AddAsync(event1);

            var event2 = IntegrationTestDataBuilder.CreateGeneralAdmissionEvent(2, venue.Id, "Event 2", 200, 75m);
            event2.Venue = venue;
            await _database.EventRepository.AddAsync(event2);

            // Create bookings for user1
            var booking1 = new Booking
            {
                User = user1,
                Event = event1,
                BookingType = BookingType.GA,
                PaymentStatus = PaymentStatus.Paid,
                TotalAmount = 50m,
                CreatedAt = DateTime.UtcNow,
                BookingItems = new List<BookingItem>()
            };

            var booking2 = new Booking
            {
                User = user1,
                Event = event2,
                BookingType = BookingType.GA,
                PaymentStatus = PaymentStatus.Paid,
                TotalAmount = 75m,
                CreatedAt = DateTime.UtcNow,
                BookingItems = new List<BookingItem>()
            };

            // Create booking for user2
            var booking3 = new Booking
            {
                User = user2,
                Event = event1,
                BookingType = BookingType.GA,
                PaymentStatus = PaymentStatus.Paid,
                TotalAmount = 50m,
                CreatedAt = DateTime.UtcNow,
                BookingItems = new List<BookingItem>()
            };

            await _database.BookingRepository.AddAsync(booking1);
            await _database.BookingRepository.AddAsync(booking2);
            await _database.BookingRepository.AddAsync(booking3);

            // Act
            var result = await _service.GetBookingsByUserIdAsync(user1.Id);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2, because: "user1 has 2 bookings");
            result.All(b => b.UserId == user1.Id).Should().BeTrue();
            result.All(b => b.UserName == "User One").Should().BeTrue();
            result.Sum(b => b.TotalAmount).Should().Be(125m);
        }

        [TestMethod]
        public async Task GetBookingsByVenueIdAsync_WithMultipleEvents_ReturnsAllVenueBookings()
        {
            // Arrange
            var venue1 = IntegrationTestDataBuilder.CreateVenue(id: 1, name: "Venue One");
            var venue2 = IntegrationTestDataBuilder.CreateVenue(id: 2, name: "Venue Two");
            var savedVenue1 = await _database.VenueRepository.AddAsync(venue1);
            var savedVenue2 = await _database.VenueRepository.AddAsync(venue2);

            var user = IntegrationTestDataBuilder.CreateUser(id: 1, name: "Test User", email: "test@example.com");
            await _database.UserRepository.AddAsync(user);

            // Events at venue1
            var event1 = IntegrationTestDataBuilder.CreateGeneralAdmissionEvent(1, savedVenue1.Id, "Event 1 at Venue 1", 100, 50m);
            event1.Venue = savedVenue1;
            await _database.EventRepository.AddAsync(event1);

            var event2 = IntegrationTestDataBuilder.CreateGeneralAdmissionEvent(2, savedVenue1.Id, "Event 2 at Venue 1", 150, 60m);
            event2.Venue = savedVenue1;
            await _database.EventRepository.AddAsync(event2);

            // Event at venue2
            var event3 = IntegrationTestDataBuilder.CreateGeneralAdmissionEvent(3, savedVenue2.Id, "Event at Venue 2", 200, 70m);
            event3.Venue = savedVenue2;
            await _database.EventRepository.AddAsync(event3);

            // Create bookings
            var booking1 = new Booking
            {
                User = user,
                Event = event1,
                BookingType = BookingType.GA,
                PaymentStatus = PaymentStatus.Paid,
                TotalAmount = 50m,
                CreatedAt = DateTime.UtcNow,
                BookingItems = new List<BookingItem>()
            };

            var booking2 = new Booking
            {
                User = user,
                Event = event2,
                BookingType = BookingType.GA,
                PaymentStatus = PaymentStatus.Paid,
                TotalAmount = 60m,
                CreatedAt = DateTime.UtcNow,
                BookingItems = new List<BookingItem>()
            };

            var booking3 = new Booking
            {
                User = user,
                Event = event3,
                BookingType = BookingType.GA,
                PaymentStatus = PaymentStatus.Paid,
                TotalAmount = 70m,
                CreatedAt = DateTime.UtcNow,
                BookingItems = new List<BookingItem>()
            };

            await _database.BookingRepository.AddAsync(booking1);
            await _database.BookingRepository.AddAsync(booking2);
            await _database.BookingRepository.AddAsync(booking3);

            // Act
            var result = await _service.GetBookingsByVenueIdAsync(savedVenue1.Id);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2, because: "venue1 has 2 events with bookings");
            result.All(b => b.VenueId == savedVenue1.Id).Should().BeTrue();
            // Venue name should be set correctly by the service
            result.All(b => !string.IsNullOrEmpty(b.VenueName)).Should().BeTrue();
            result.Sum(b => b.TotalAmount).Should().Be(110m);
        }

        [TestMethod]
        public async Task GetBookingByIdAsync_ExistingBooking_ReturnsCompleteDetails()
        {
            // Arrange
            var venue = IntegrationTestDataBuilder.CreateVenue(id: 1, name: "Concert Hall");
            var savedVenue = await _database.VenueRepository.AddAsync(venue);

            var user = IntegrationTestDataBuilder.CreateUser(id: 1, name: "Concert Goer", email: "goer@example.com");
            await _database.UserRepository.AddAsync(user);

            var evnt = IntegrationTestDataBuilder.CreateGeneralAdmissionEvent(1, savedVenue.Id, "Rock Festival", 1000, 85m);
            evnt.Venue = savedVenue;
            await _database.EventRepository.AddAsync(evnt);

            var booking = new Booking
            {
                User = user,
                Event = evnt,
                BookingType = BookingType.GA,
                PaymentStatus = PaymentStatus.Paid,
                TotalAmount = 170m,
                CreatedAt = DateTime.UtcNow,
                BookingItems = new List<BookingItem>()
            };

            var savedBooking = await _database.BookingRepository.AddAsync(booking);

            // Act
            var result = await _service.GetBookingByIdAsync(savedBooking.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(savedBooking.Id);
            result.UserId.Should().Be(user.Id);
            result.UserName.Should().Be("Concert Goer");
            result.UserEmail.Should().Be("goer@example.com");
            result.EventId.Should().Be(evnt.Id);
            result.EventName.Should().Be("Rock Festival");
            result.VenueId.Should().Be(savedVenue.Id);
            // Venue name should be loaded by the service
            result.VenueName.Should().NotBeNullOrEmpty();
            result.BookingType.Should().Be("GA");
            result.PaymentStatus.Should().Be("Paid");
            result.TotalAmount.Should().Be(170m);
        }

        [TestMethod]
        public async Task GetBookingsByUserIdAsync_NoBookings_ReturnsEmptyCollection()
        {
            // Arrange
            var user = IntegrationTestDataBuilder.CreateUser(id: 1, name: "New User", email: "new@example.com");
            await _database.UserRepository.AddAsync(user);

            // Act
            var result = await _service.GetBookingsByUserIdAsync(user.Id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [TestMethod]
        public async Task GetBookingsByVenueIdAsync_NoEvents_ReturnsEmptyCollection()
        {
            // Arrange
            var venue = IntegrationTestDataBuilder.CreateVenue(id: 1, name: "New Venue");
            await _database.VenueRepository.AddAsync(venue);

            // Act
            var result = await _service.GetBookingsByVenueIdAsync(venue.Id);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [TestMethod]
        public async Task GetBookingByIdAsync_NonExistentBooking_ReturnsNull()
        {
            // Arrange
            var nonExistentId = 999;

            // Act
            var result = await _service.GetBookingByIdAsync(nonExistentId);

            // Assert
            result.Should().BeNull();
        }
    }
}
