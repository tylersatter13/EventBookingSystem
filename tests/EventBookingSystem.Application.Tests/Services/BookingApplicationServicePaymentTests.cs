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
    /// <summary>
    /// Tests for payment failure scenarios in BookingApplicationService.
    /// </summary>
    [TestClass]
    public class BookingApplicationServicePaymentTests
    {
        private Mock<IUserRepository> _mockUserRepository = null!;
        private Mock<IEventRepository> _mockEventRepository = null!;
        private Mock<IVenueRepository> _mockVenueRepository = null!;
        private Mock<IBookingRepository> _mockBookingRepository = null!;
        private Mock<IBookingService> _mockBookingService = null!;
        private Mock<IPaymentService> _mockPaymentService = null!;
        private BookingApplicationService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockEventRepository = new Mock<IEventRepository>();
            _mockVenueRepository = new Mock<IVenueRepository>();
            _mockBookingRepository = new Mock<IBookingRepository>();
            _mockBookingService = new Mock<IBookingService>();
            _mockPaymentService = new Mock<IPaymentService>();

            _service = new BookingApplicationService(
                _mockBookingRepository.Object,
                _mockEventRepository.Object,
                _mockUserRepository.Object,
                _mockVenueRepository.Object,
                _mockBookingService.Object,
                _mockPaymentService.Object
            );
        }

        [TestMethod]
        public async Task CreateBookingAsync_PaymentFails_ReturnsFailure()
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

            var booking = new Booking
            {
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
                .Returns(booking);

            // Setup payment to fail
            _mockPaymentService
                .Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(PaymentResult.Failure("Insufficient funds"));

            // Act
            var result = await _service.CreateBookingAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessful.Should().BeFalse();
            result.Message.Should().Contain("Payment failed");
            result.Message.Should().Contain("Insufficient funds");
            result.BookingId.Should().BeNull();

            // Verify payment was attempted
            _mockPaymentService.Verify(
                x => x.ProcessPaymentAsync(
                    It.Is<PaymentRequest>(r => r.UserId == userId && r.Amount == 100m),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // Verify booking was NOT persisted after payment failure
            _mockBookingRepository.Verify(
                x => x.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()),
                Times.Never);

            // Verify event was NOT updated after payment failure
            _mockEventRepository.Verify(
                x => x.UpdateAsync(It.IsAny<EventBase>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [TestMethod]
        public async Task CreateBookingAsync_PaymentSucceeds_BookingStatusIsPaid()
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

            var booking = new Booking
            {
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
                .Returns(booking);

            // Setup payment to succeed
            _mockPaymentService
                .Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(PaymentResult.Success("TXN-12345"));

            _mockBookingRepository
                .Setup(x => x.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Booking b, CancellationToken ct) => { b.Id = 999; return b; });

            _mockEventRepository
                .Setup(x => x.UpdateAsync(evnt, It.IsAny<CancellationToken>()))
                .ReturnsAsync(evnt);

            // Act
            var result = await _service.CreateBookingAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessful.Should().BeTrue();
            result.BookingId.Should().Be(999);

            // Verify the booking was saved with PaymentStatus = Paid
            _mockBookingRepository.Verify(
                x => x.AddAsync(
                    It.Is<Booking>(b => b.PaymentStatus == PaymentStatus.Paid),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task CreateBookingAsync_PaymentExceedsLimit_ReturnsFailure()
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
                Name = "VIP Concert",
                Address = "123 Main St",
                VenueSections = new List<VenueSection>
                {
                    new VenueSection
                    {
                        Id = 1,
                        Name = "VIP",
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
                Name = "VIP Concert",
                StartsAt = DateTime.UtcNow.AddDays(30),
                Capacity = 1000,
                Price = 12000m, // Exceeds $10,000 limit
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
                TotalAmount = 12000m,
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
                .Returns(booking);

            // Setup payment to fail due to exceeding limit
            _mockPaymentService
                .Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(PaymentResult.Failure("Payment amount exceeds maximum limit of $10,000"));

            // Act
            var result = await _service.CreateBookingAsync(command);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccessful.Should().BeFalse();
            result.Message.Should().Contain("Payment failed");
            result.Message.Should().Contain("exceeds maximum limit");
        }
    }
}
