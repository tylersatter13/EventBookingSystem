using AwesomeAssertions;
using EventBookingSystem.Application.Models;
using EventBookingSystem.Application.Services;
using EventBookingSystem.Domain;
using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Domain.Services;
using EventBookingSystem.Infrastructure.Interfaces;
using Moq;

namespace EventBookingSystem.Application.Tests.Services
{
    [TestClass]
    public class BookingApplicationServiceTests
    {
        private Mock<IUserRepository> _mockUserRepository;
        private Mock<IEventRepository> _mockEventRepository;
        private Mock<IVenueRepository> _mockVenueRepository;
        private Mock<IBookingRepository> _mockBookingRepository;
        private Mock<IBookingService> _mockBookingService;
        private BookingApplicationService _service;

        public BookingApplicationServiceTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockEventRepository = new Mock<IEventRepository>();
            _mockVenueRepository = new Mock<IVenueRepository>();
            _mockBookingRepository = new Mock<IBookingRepository>();
            
            // Create a real EventReservationService for BookingService
            var reservationService = new EventReservationService();
            _mockBookingService = new Mock<IBookingService>();

            // Use deterministic payment service for tests
            var paymentService = new DeterministicPaymentService();

            _service = new BookingApplicationService(
                _mockBookingRepository.Object,
                _mockEventRepository.Object,
                _mockUserRepository.Object,
                _mockVenueRepository.Object,
                _mockBookingService.Object,
                paymentService
            );
        }

        [TestMethod]
        public async Task CreateBookingAsync_ValidGeneralAdmissionRequest_CreatesBookingSuccessfully()
        {
            // Arrange
            var userId = 1;
            var eventId = 10;
            var venueId = 100;

            var user = new User
            {
                Id = userId,
                Name = "John Doe",
                Email = "john@example.com"
            };

            var venue = new Venue
            {
                Id = venueId,
                Name = "Concert Hall",
                Address = "123 Main St",
                VenueSections = new List<VenueSection>
                {
                    new VenueSection
                    {
                        Id = 1,
                        Name = "Main Floor",
                        VenueSeats = new List<VenueSeat>
                        {
                            new VenueSeat { Id = 1, Row = "A", SeatNumber = "1" }
                        }
                    }
                }
            };

            var evnt = new GeneralAdmissionEvent
            {
                Id = eventId,
                VenueId = venueId,
                Name = "Rock Concert",
                StartsAt = DateTime.UtcNow.AddDays(30),
                Capacity = 1000,
                Price = 50m,
                Venue = venue
            };

            var command = new CreateBookingCommand
            {
                UserId = userId,
                EventId = eventId,
                Quantity = 2
            };

            var expectedBooking = new Booking
            {
                Id = 999,
                User = user,
                Event = evnt,
                BookingType = BookingType.GA,
                TotalAmount = 100m,
                PaymentStatus = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                BookingItems = new List<BookingItem>()
            };

            // Setup mocks
            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _mockEventRepository
                .Setup(x => x.GetByIdWithDetailsAsync(eventId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(evnt);

            _mockVenueRepository
                .Setup(x => x.GetByIdAsync(venueId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(venue);

            _mockBookingService
                .Setup(x => x.ValidateBooking(user, evnt, It.IsAny<ReservationRequest>()))
                .Returns(ValidationResult.Success());

            _mockBookingService
                .Setup(x => x.CreateBooking(user, evnt, It.IsAny<ReservationRequest>()))
                .Returns(expectedBooking);

            _mockBookingRepository
                .Setup(x => x.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedBooking);

            _mockEventRepository
                .Setup(x => x.UpdateAsync(evnt, It.IsAny<CancellationToken>()))
                .ReturnsAsync(evnt);

            // Act
            var result = await _service.CreateBookingAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessful.Should().BeTrue();
            result.BookingId.Should().Be(999);
            result.TotalAmount.Should().Be(100m);
            result.Message.Should().Contain("successfully");

            // Verify all required calls were made
            _mockUserRepository.Verify(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
            _mockEventRepository.Verify(x => x.GetByIdWithDetailsAsync(eventId, It.IsAny<CancellationToken>()), Times.Once);
            _mockVenueRepository.Verify(x => x.GetByIdAsync(venueId, It.IsAny<CancellationToken>()), Times.Once);
            _mockBookingService.Verify(x => x.ValidateBooking(user, evnt, It.IsAny<ReservationRequest>()), Times.Once);
            _mockBookingService.Verify(x => x.CreateBooking(user, evnt, It.IsAny<ReservationRequest>()), Times.Once);
            _mockBookingRepository.Verify(x => x.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task CreateBookingAsync_InvalidUser_ReturnsFailure()
        {
            // Arrange
            var command = new CreateBookingCommand
            {
                UserId = 999,
                EventId = 10,
                Quantity = 2
            };

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User)null);

            // Act
            var result = await _service.CreateBookingAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessful.Should().BeFalse();
            result.Message.Should().Contain("User not found");
            result.BookingId.Should().BeNull();

            // Verify only user lookup was attempted
            _mockUserRepository.Verify(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
            _mockEventRepository.Verify(x => x.GetByIdWithDetailsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockBookingRepository.Verify(x => x.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task CreateBookingAsync_InvalidEvent_ReturnsFailure()
        {
            // Arrange
            var userId = 1;
            var user = new User
            {
                Id = userId,
                Name = "John Doe",
                Email = "john@example.com"
            };

            var command = new CreateBookingCommand
            {
                UserId = userId,
                EventId = 999,
                Quantity = 2
            };

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _mockEventRepository
                .Setup(x => x.GetByIdWithDetailsAsync(999, It.IsAny<CancellationToken>()))
                .ReturnsAsync((EventBase)null);

            // Act
            var result = await _service.CreateBookingAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessful.Should().BeFalse();
            result.Message.Should().Contain("Event not found");
            result.BookingId.Should().BeNull();

            // Verify user and event lookups were attempted
            _mockUserRepository.Verify(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
            _mockEventRepository.Verify(x => x.GetByIdWithDetailsAsync(999, It.IsAny<CancellationToken>()), Times.Once);
            _mockVenueRepository.Verify(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockBookingRepository.Verify(x => x.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task CreateBookingAsync_InvalidVenue_ReturnsFailure()
        {
            // Arrange
            var userId = 1;
            var eventId = 10;
            var venueId = 999;

            var user = new User
            {
                Id = userId,
                Name = "John Doe",
                Email = "john@example.com"
            };

            var evnt = new GeneralAdmissionEvent
            {
                Id = eventId,
                VenueId = venueId,
                Name = "Concert",
                StartsAt = DateTime.UtcNow.AddDays(30),
                Capacity = 1000
            };

            var command = new CreateBookingCommand
            {
                UserId = userId,
                EventId = eventId,
                Quantity = 2
            };

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _mockEventRepository
                .Setup(x => x.GetByIdWithDetailsAsync(eventId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(evnt);

            _mockVenueRepository
                .Setup(x => x.GetByIdAsync(venueId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Venue)null);

            // Act
            var result = await _service.CreateBookingAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessful.Should().BeFalse();
            result.Message.Should().Contain("Venue not found");
            result.BookingId.Should().BeNull();

            // Verify lookups were attempted in order
            _mockUserRepository.Verify(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
            _mockEventRepository.Verify(x => x.GetByIdWithDetailsAsync(eventId, It.IsAny<CancellationToken>()), Times.Once);
            _mockVenueRepository.Verify(x => x.GetByIdAsync(venueId, It.IsAny<CancellationToken>()), Times.Once);
            _mockBookingRepository.Verify(x => x.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task CreateBookingAsync_ValidationFails_ReturnsFailure()
        {
            // Arrange
            var userId = 1;
            var eventId = 10;
            var venueId = 100;

            var user = new User
            {
                Id = userId,
                Name = "John Doe",
                Email = "john@example.com"
            };

            var venue = new Venue
            {
                Id = venueId,
                Name = "Small Venue",
                Address = "123 Main St",
                VenueSections = new List<VenueSection>
                {
                    new VenueSection
                    {
                        Id = 1,
                        Name = "Main",
                        VenueSeats = new List<VenueSeat>
                        {
                            new VenueSeat { Id = 1, Row = "A", SeatNumber = "1" }
                        }
                    }
                }
            };

            var evnt = new GeneralAdmissionEvent
            {
                Id = eventId,
                VenueId = venueId,
                Name = "Sold Out Show",
                StartsAt = DateTime.UtcNow.AddDays(30),
                Capacity = 10,
                Price = 50m,
                Venue = venue
            };
            evnt.ReserveTickets(10); // Sell out the event

            var command = new CreateBookingCommand
            {
                UserId = userId,
                EventId = eventId,
                Quantity = 2
            };

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _mockEventRepository
                .Setup(x => x.GetByIdWithDetailsAsync(eventId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(evnt);

            _mockVenueRepository
                .Setup(x => x.GetByIdAsync(venueId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(venue);

            _mockBookingService
                .Setup(x => x.ValidateBooking(user, evnt, It.IsAny<ReservationRequest>()))
                .Returns(ValidationResult.Failure("Event is sold out"));

            // Act
            var result = await _service.CreateBookingAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessful.Should().BeFalse();
            result.Message.Should().Contain("Event is sold out");
            result.BookingId.Should().BeNull();

            // Verify validation was called but booking was not created
            _mockBookingService.Verify(x => x.ValidateBooking(user, evnt, It.IsAny<ReservationRequest>()), Times.Once);
            _mockBookingService.Verify(x => x.CreateBooking(It.IsAny<User>(), It.IsAny<EventBase>(), It.IsAny<ReservationRequest>()), Times.Never);
            _mockBookingRepository.Verify(x => x.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestMethod]
        public async Task CreateBookingAsync_SectionBasedEvent_CreatesBookingSuccessfully()
        {
            // Arrange
            var userId = 1;
            var eventId = 10;
            var venueId = 100;
            var sectionId = 1;

            var user = new User
            {
                Id = userId,
                Name = "Jane Smith",
                Email = "jane@example.com"
            };

            var venue = new Venue
            {
                Id = venueId,
                Name = "Stadium",
                Address = "456 Sports Ave",
                VenueSections = new List<VenueSection>
                {
                    new VenueSection
                    {
                        Id = sectionId,
                        Name = "VIP Section",
                        VenueSeats = Enumerable.Range(1, 500).Select(i => new VenueSeat
                        {
                            Id = i,
                            Row = "A",
                            SeatNumber = i.ToString()
                        }).ToList()
                    }
                }
            };

            var evnt = new SectionBasedEvent
            {
                Id = eventId,
                VenueId = venueId,
                Name = "Football Game",
                StartsAt = DateTime.UtcNow.AddDays(7),
                Venue = venue,
                SectionInventories = new List<EventSectionInventory>
                {
                    new EventSectionInventory
                    {
                        VenueSectionId = sectionId,
                        Capacity = 500,
                        Price = 100m,
                        VenueSection = venue.VenueSections.First()
                    }
                }
            };

            var command = new CreateBookingCommand
            {
                UserId = userId,
                EventId = eventId,
                Quantity = 3,
                SectionId = sectionId
            };

            var expectedBooking = new Booking
            {
                Id = 888,
                User = user,
                Event = evnt,
                BookingType = BookingType.Section,
                TotalAmount = 300m,
                PaymentStatus = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                BookingItems = new List<BookingItem>()
            };

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _mockEventRepository
                .Setup(x => x.GetByIdWithDetailsAsync(eventId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(evnt);

            _mockVenueRepository
                .Setup(x => x.GetByIdAsync(venueId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(venue);

            _mockBookingService
                .Setup(x => x.ValidateBooking(user, evnt, It.IsAny<ReservationRequest>()))
                .Returns(ValidationResult.Success());

            _mockBookingService
                .Setup(x => x.CreateBooking(user, evnt, It.IsAny<ReservationRequest>()))
                .Returns(expectedBooking);

            _mockBookingRepository
                .Setup(x => x.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedBooking);

            _mockEventRepository
                .Setup(x => x.UpdateAsync(evnt, It.IsAny<CancellationToken>()))
                .ReturnsAsync(evnt);

            // Act
            var result = await _service.CreateBookingAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessful.Should().BeTrue();
            result.BookingId.Should().Be(888);
            result.TotalAmount.Should().Be(300m);
            result.Message.Should().Contain("successfully");

            // Verify the reservation request was created with correct section ID
            _mockBookingService.Verify(x => x.ValidateBooking(
                user,
                evnt,
                It.Is<ReservationRequest>(r => r.SectionId == sectionId && r.Quantity == 3)), 
                Times.Once);
        }

        [TestMethod]
        public async Task CreateBookingAsync_ReservedSeatingEvent_CreatesBookingSuccessfully()
        {
            // Arrange
            var userId = 1;
            var eventId = 10;
            var venueId = 100;
            var seatId = 42;

            var user = new User
            {
                Id = userId,
                Name = "Bob Johnson",
                Email = "bob@example.com"
            };

            var venue = new Venue
            {
                Id = venueId,
                Name = "Theatre",
                Address = "789 Broadway",
                VenueSections = new List<VenueSection>
                {
                    new VenueSection
                    {
                        Id = 1,
                        Name = "Orchestra",
                        VenueSeats = new List<VenueSeat>
                        {
                            new VenueSeat { Id = seatId, Row = "A", SeatNumber = "15", SeatLabel = "A15" }
                        }
                    }
                }
            };

            var evnt = new ReservedSeatingEvent
            {
                Id = eventId,
                VenueId = venueId,
                Name = "Theatre Show",
                StartsAt = DateTime.UtcNow.AddDays(14),
                Venue = venue,
                Seats = new List<EventSeat>
                {
                    new EventSeat
                    {
                        Id = 1,
                        VenueSeatId = seatId,
                        Status = SeatStatus.Available,
                        VenueSeat = venue.VenueSections.First().VenueSeats.First()
                    }
                }
            };

            var command = new CreateBookingCommand
            {
                UserId = userId,
                EventId = eventId,
                Quantity = 1,
                SeatId = seatId
            };

            var expectedBooking = new Booking
            {
                Id = 777,
                User = user,
                Event = evnt,
                BookingType = BookingType.Seat,
                TotalAmount = 75m,
                PaymentStatus = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                BookingItems = new List<BookingItem>()
            };

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _mockEventRepository
                .Setup(x => x.GetByIdWithDetailsAsync(eventId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(evnt);

            _mockVenueRepository
                .Setup(x => x.GetByIdAsync(venueId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(venue);

            _mockBookingService
                .Setup(x => x.ValidateBooking(user, evnt, It.IsAny<ReservationRequest>()))
                .Returns(ValidationResult.Success());

            _mockBookingService
                .Setup(x => x.CreateBooking(user, evnt, It.IsAny<ReservationRequest>()))
                .Returns(expectedBooking);

            _mockBookingRepository
                .Setup(x => x.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedBooking);

            _mockEventRepository
                .Setup(x => x.UpdateAsync(evnt, It.IsAny<CancellationToken>()))
                .ReturnsAsync(evnt);

            // Act
            var result = await _service.CreateBookingAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessful.Should().BeTrue();
            result.BookingId.Should().Be(777);
            result.TotalAmount.Should().Be(75m);

            // Verify the reservation request was created with correct seat ID
            _mockBookingService.Verify(x => x.ValidateBooking(
                user,
                evnt,
                It.Is<ReservationRequest>(r => r.SeatId == seatId)), 
                Times.Once);
        }

        [TestMethod]
        public async Task CreateBookingAsync_RepositoryAddFails_ShouldReturnUnsuccessful()
        {
            // Arrange
            var userId = 1;
            var eventId = 10;
            var venueId = 100;

            var user = new User { Id = userId, Name = "Test User", Email = "test@example.com" };
            var venue = new Venue
            {
                Id = venueId,
                Name = "Venue",
                Address = "Address",
                VenueSections = new List<VenueSection>
                {
                    new VenueSection
                    {
                        Id = 1,
                        Name = "Main",
                        VenueSeats = new List<VenueSeat> { new VenueSeat { Id = 1, Row = "A", SeatNumber = "1" } }
                    }
                }
            };

            var evnt = new GeneralAdmissionEvent
            {
                Id = eventId,
                VenueId = venueId,
                Name = "Event",
                StartsAt = DateTime.UtcNow.AddDays(30),
                Capacity = 100,
                Venue = venue
            };

            var command = new CreateBookingCommand
            {
                UserId = userId,
                EventId = eventId,
                Quantity = 1
            };

            var booking = new Booking
            {
                User = user,
                Event = evnt,
                BookingType = BookingType.GA,
                TotalAmount = 50m,
                PaymentStatus = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                BookingItems = new List<BookingItem>()
            };

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            _mockEventRepository
                .Setup(x => x.GetByIdWithDetailsAsync(eventId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(evnt);

            _mockVenueRepository
                .Setup(x => x.GetByIdAsync(venueId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(venue);

            _mockBookingService
                .Setup(x => x.ValidateBooking(user, evnt, It.IsAny<ReservationRequest>()))
                .Returns(ValidationResult.Success());

            _mockBookingService
                .Setup(x => x.CreateBooking(user, evnt, It.IsAny<ReservationRequest>()))
                .Returns(booking);

            _mockBookingRepository
                .Setup(x => x.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Booking)null); // Simulate failure

            // Act
            var result = await _service.CreateBookingAsync(command);

            // Assert
            result.IsSuccessful.Should().BeFalse();
            result.Message.Should().Contain("Failed to create booking");
            _mockBookingRepository.Verify(x => x.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockEventRepository.Verify(x => x.UpdateAsync(It.IsAny<EventBase>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
