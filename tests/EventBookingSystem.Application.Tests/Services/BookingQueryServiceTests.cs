using AwesomeAssertions;
using EventBookingSystem.Application.Services;
using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Infrastructure.Interfaces;
using Moq;

namespace EventBookingSystem.Application.Tests.Services
{
    [TestClass]
    public class BookingQueryServiceTests
    {
        private Mock<IBookingRepository> _mockBookingRepository = null!;
        private Mock<IEventRepository> _mockEventRepository = null!;
        private BookingQueryService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockBookingRepository = new Mock<IBookingRepository>();
            _mockEventRepository = new Mock<IEventRepository>();
            _service = new BookingQueryService(_mockBookingRepository.Object, _mockEventRepository.Object);
        }

        #region GetBookingsByUserIdAsync Tests

        [TestMethod]
        public async Task GetBookingsByUserIdAsync_ValidUserId_ReturnsBookings()
        {
            // Arrange
            var userId = 1;
            var user = new User { Id = userId, Name = "John Doe", Email = "john@example.com" };
            var venue = new Venue { Id = 1, Name = "Test Venue", Address = "123 Test St" };
            var evnt = new GeneralAdmissionEvent
            {
                Id = 1,
                VenueId = 1,
                Name = "Test Event",
                StartsAt = DateTime.UtcNow.AddDays(7),
                Capacity = 100,
                Venue = venue
            };

            var bookings = new List<Booking>
            {
                new Booking
                {
                    Id = 1,
                    User = user,
                    Event = evnt,
                    BookingType = BookingType.GA,
                    PaymentStatus = PaymentStatus.Paid,
                    TotalAmount = 100m,
                    CreatedAt = DateTime.UtcNow,
                    BookingItems = new List<BookingItem>()
                },
                new Booking
                {
                    Id = 2,
                    User = user,
                    Event = evnt,
                    BookingType = BookingType.GA,
                    PaymentStatus = PaymentStatus.Paid,
                    TotalAmount = 50m,
                    CreatedAt = DateTime.UtcNow,
                    BookingItems = new List<BookingItem>()
                }
            };

            _mockBookingRepository
                .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(bookings);

            // Act
            var result = await _service.GetBookingsByUserIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.First().UserId.Should().Be(userId);
            result.First().UserName.Should().Be("John Doe");
            result.First().TotalAmount.Should().Be(100m);
            
            _mockBookingRepository.Verify(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task GetBookingsByUserIdAsync_InvalidUserId_ThrowsArgumentException()
        {
            // Arrange
            var userId = 0;

            // Act
            Func<Task> act = async () => await _service.GetBookingsByUserIdAsync(userId);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("User ID must be greater than zero*");
        }

        [TestMethod]
        public async Task GetBookingsByUserIdAsync_NegativeUserId_ThrowsArgumentException()
        {
            // Arrange
            var userId = -1;

            // Act
            Func<Task> act = async () => await _service.GetBookingsByUserIdAsync(userId);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [TestMethod]
        public async Task GetBookingsByUserIdAsync_NoBookings_ReturnsEmptyCollection()
        {
            // Arrange
            var userId = 1;
            _mockBookingRepository
                .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Booking>());

            // Act
            var result = await _service.GetBookingsByUserIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        #endregion

        #region GetBookingsByVenueIdAsync Tests

        [TestMethod]
        public async Task GetBookingsByVenueIdAsync_ValidVenueId_ReturnsBookings()
        {
            // Arrange
            var venueId = 1;
            var user = new User { Id = 1, Name = "Jane Smith", Email = "jane@example.com" };
            var venue = new Venue { Id = venueId, Name = "Test Venue", Address = "456 Venue St" };
            
            var event1 = new GeneralAdmissionEvent
            {
                Id = 1,
                VenueId = venueId,
                Name = "Event 1",
                StartsAt = DateTime.UtcNow.AddDays(7),
                Capacity = 100,
                Venue = venue
            };

            var event2 = new GeneralAdmissionEvent
            {
                Id = 2,
                VenueId = venueId,
                Name = "Event 2",
                StartsAt = DateTime.UtcNow.AddDays(14),
                Capacity = 150,
                Venue = venue
            };

            var events = new List<EventBase> { event1, event2 };

            var bookingsForEvent1 = new List<Booking>
            {
                new Booking
                {
                    Id = 1,
                    User = user,
                    Event = event1,
                    BookingType = BookingType.GA,
                    PaymentStatus = PaymentStatus.Paid,
                    TotalAmount = 75m,
                    CreatedAt = DateTime.UtcNow,
                    BookingItems = new List<BookingItem>()
                }
            };

            var bookingsForEvent2 = new List<Booking>
            {
                new Booking
                {
                    Id = 2,
                    User = user,
                    Event = event2,
                    BookingType = BookingType.GA,
                    PaymentStatus = PaymentStatus.Paid,
                    TotalAmount = 150m,
                    CreatedAt = DateTime.UtcNow,
                    BookingItems = new List<BookingItem>()
                }
            };

            _mockEventRepository
                .Setup(x => x.GetByVenueIdAsync(venueId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(events);

            _mockBookingRepository
                .Setup(x => x.GetByEventIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(bookingsForEvent1);

            _mockBookingRepository
                .Setup(x => x.GetByEventIdAsync(2, It.IsAny<CancellationToken>()))
                .ReturnsAsync(bookingsForEvent2);

            // Act
            var result = await _service.GetBookingsByVenueIdAsync(venueId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.All(b => b.VenueId == venueId).Should().BeTrue();
            result.First().VenueName.Should().Be("Test Venue");
            
            _mockEventRepository.Verify(x => x.GetByVenueIdAsync(venueId, It.IsAny<CancellationToken>()), Times.Once);
            _mockBookingRepository.Verify(x => x.GetByEventIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [TestMethod]
        public async Task GetBookingsByVenueIdAsync_InvalidVenueId_ThrowsArgumentException()
        {
            // Arrange
            var venueId = 0;

            // Act
            Func<Task> act = async () => await _service.GetBookingsByVenueIdAsync(venueId);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Venue ID must be greater than zero*");
        }

        [TestMethod]
        public async Task GetBookingsByVenueIdAsync_NoEventsAtVenue_ReturnsEmptyCollection()
        {
            // Arrange
            var venueId = 1;
            _mockEventRepository
                .Setup(x => x.GetByVenueIdAsync(venueId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<EventBase>());

            // Act
            var result = await _service.GetBookingsByVenueIdAsync(venueId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
            
            // Should not query bookings if no events exist
            _mockBookingRepository.Verify(x => x.GetByEventIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region GetBookingByIdAsync Tests

        [TestMethod]
        public async Task GetBookingByIdAsync_ValidBookingId_ReturnsBooking()
        {
            // Arrange
            var bookingId = 1;
            var user = new User { Id = 1, Name = "Alice Brown", Email = "alice@example.com" };
            var venue = new Venue { Id = 1, Name = "Concert Hall", Address = "789 Music St" };
            var evnt = new GeneralAdmissionEvent
            {
                Id = 1,
                VenueId = 1,
                Name = "Rock Concert",
                StartsAt = DateTime.UtcNow.AddDays(7),
                Capacity = 500,
                Venue = venue
            };

            var booking = new Booking
            {
                Id = bookingId,
                User = user,
                Event = evnt,
                BookingType = BookingType.GA,
                PaymentStatus = PaymentStatus.Paid,
                TotalAmount = 120m,
                CreatedAt = DateTime.UtcNow,
                BookingItems = new List<BookingItem>()
            };

            _mockBookingRepository
                .Setup(x => x.GetByIdAsync(bookingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(booking);

            // Act
            var result = await _service.GetBookingByIdAsync(bookingId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(bookingId);
            result.UserName.Should().Be("Alice Brown");
            result.EventName.Should().Be("Rock Concert");
            result.TotalAmount.Should().Be(120m);
            
            _mockBookingRepository.Verify(x => x.GetByIdAsync(bookingId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestMethod]
        public async Task GetBookingByIdAsync_InvalidBookingId_ThrowsArgumentException()
        {
            // Arrange
            var bookingId = 0;

            // Act
            Func<Task> act = async () => await _service.GetBookingByIdAsync(bookingId);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Booking ID must be greater than zero*");
        }

        [TestMethod]
        public async Task GetBookingByIdAsync_BookingNotFound_ReturnsNull()
        {
            // Arrange
            var bookingId = 999;
            _mockBookingRepository
                .Setup(x => x.GetByIdAsync(bookingId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Booking?)null);

            // Act
            var result = await _service.GetBookingByIdAsync(bookingId);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region GetBookingsForPaidUsersAtVenueAsync Tests

        [TestMethod]
        public async Task GetBookingsForPaidUsersAtVenueAsync_ValidVenueId_ReturnsBookings()
        {
            // Arrange
            var venueId = 1;
            var user = new User { Id = 1, Name = "Paid User", Email = "paid@example.com" };
            var venue = new Venue { Id = venueId, Name = "Test Venue", Address = "123 Venue St" };
            var evnt = new GeneralAdmissionEvent
            {
                Id = 1,
                VenueId = venueId,
                Name = "Test Event",
                StartsAt = DateTime.UtcNow.AddDays(7),
                Capacity = 100,
                Venue = venue
            };

            var booking1 = new Booking
            {
                Id = 1,
                User = user,
                Event = evnt,
                BookingType = BookingType.GA,
                PaymentStatus = PaymentStatus.Paid,
                TotalAmount = 100m,
                CreatedAt = DateTime.UtcNow,
                BookingItems = new List<BookingItem>()
            };

            var booking2 = new Booking
            {
                Id = 2,
                User = user,
                Event = evnt,
                BookingType = BookingType.GA,
                PaymentStatus = PaymentStatus.Pending,
                TotalAmount = 75m,
                CreatedAt = DateTime.UtcNow.AddHours(1),
                BookingItems = new List<BookingItem>()
            };

            _mockBookingRepository
                .Setup(x => x.FindBookingsForPaidUsersAtVenueAsync(venueId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Booking> { booking1, booking2 });

            // Act
            var result = await _service.GetBookingsForPaidUsersAtVenueAsync(venueId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.All(b => b.VenueId == venueId).Should().BeTrue();
            result.Should().Contain(b => b.PaymentStatus == "Paid");
            result.Should().Contain(b => b.PaymentStatus == "Pending");
            
            _mockBookingRepository.Verify(
                x => x.FindBookingsForPaidUsersAtVenueAsync(venueId, It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [TestMethod]
        public async Task GetBookingsForPaidUsersAtVenueAsync_InvalidVenueId_ThrowsArgumentException()
        {
            // Arrange
            var venueId = 0;

            // Act
            Func<Task> act = async () => await _service.GetBookingsForPaidUsersAtVenueAsync(venueId);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Venue ID must be greater than zero*");
        }

        [TestMethod]
        public async Task GetBookingsForPaidUsersAtVenueAsync_NegativeVenueId_ThrowsArgumentException()
        {
            // Arrange
            var venueId = -5;

            // Act
            Func<Task> act = async () => await _service.GetBookingsForPaidUsersAtVenueAsync(venueId);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [TestMethod]
        public async Task GetBookingsForPaidUsersAtVenueAsync_NoQualifyingBookings_ReturnsEmptyCollection()
        {
            // Arrange
            var venueId = 1;
            _mockBookingRepository
                .Setup(x => x.FindBookingsForPaidUsersAtVenueAsync(venueId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Booking>());

            // Act
            var result = await _service.GetBookingsForPaidUsersAtVenueAsync(venueId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [TestMethod]
        public async Task GetBookingsForPaidUsersAtVenueAsync_MapsAllBookingProperties()
        {
            // Arrange
            var venueId = 1;
            var user = new User { Id = 1, Name = "John Doe", Email = "john@example.com" };
            var venue = new Venue { Id = venueId, Name = "Stadium", Address = "Sports Ave" };
            var evnt = new GeneralAdmissionEvent
            {
                Id = 10,
                VenueId = venueId,
                Name = "Big Game",
                StartsAt = DateTime.UtcNow.AddDays(14),
                Capacity = 500,
                Venue = venue
            };

            var booking = new Booking
            {
                Id = 5,
                User = user,
                Event = evnt,
                BookingType = BookingType.GA,
                PaymentStatus = PaymentStatus.Paid,
                TotalAmount = 250m,
                CreatedAt = DateTime.UtcNow.AddHours(-5),
                BookingItems = new List<BookingItem>()
            };

            _mockBookingRepository
                .Setup(x => x.FindBookingsForPaidUsersAtVenueAsync(venueId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Booking> { booking });

            // Act
            var result = await _service.GetBookingsForPaidUsersAtVenueAsync(venueId);

            // Assert
            var dto = result.First();
            dto.Id.Should().Be(5);
            dto.UserId.Should().Be(1);
            dto.UserName.Should().Be("John Doe");
            dto.UserEmail.Should().Be("john@example.com");
            dto.EventId.Should().Be(10);
            dto.EventName.Should().Be("Big Game");
            dto.VenueId.Should().Be(venueId);
            dto.VenueName.Should().Be("Stadium");
            dto.BookingType.Should().Be("GA");
            dto.PaymentStatus.Should().Be("Paid");
            dto.TotalAmount.Should().Be(250m);
        }

        #endregion

        #region GetUsersWithoutBookingsAtVenueAsync Tests

        [TestMethod]
        public async Task GetUsersWithoutBookingsAtVenueAsync_ValidVenueId_ReturnsUserIds()
        {
            // Arrange
            var venueId = 1;
            var userIds = new List<int> { 2, 3, 5, 8 };

            _mockBookingRepository
                .Setup(x => x.FindUsersWithoutBookingsInVenueAsync(venueId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(userIds);

            // Act
            var result = await _service.GetUsersWithoutBookingsAtVenueAsync(venueId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(4);
            result.Should().Contain(2);
            result.Should().Contain(3);
            result.Should().Contain(5);
            result.Should().Contain(8);
            
            _mockBookingRepository.Verify(
                x => x.FindUsersWithoutBookingsInVenueAsync(venueId, It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [TestMethod]
        public async Task GetUsersWithoutBookingsAtVenueAsync_InvalidVenueId_ThrowsArgumentException()
        {
            // Arrange
            var venueId = 0;

            // Act
            Func<Task> act = async () => await _service.GetUsersWithoutBookingsAtVenueAsync(venueId);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("Venue ID must be greater than zero*");
        }

        [TestMethod]
        public async Task GetUsersWithoutBookingsAtVenueAsync_NegativeVenueId_ThrowsArgumentException()
        {
            // Arrange
            var venueId = -10;

            // Act
            Func<Task> act = async () => await _service.GetUsersWithoutBookingsAtVenueAsync(venueId);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [TestMethod]
        public async Task GetUsersWithoutBookingsAtVenueAsync_NoUsers_ReturnsEmptyCollection()
        {
            // Arrange
            var venueId = 1;
            _mockBookingRepository
                .Setup(x => x.FindUsersWithoutBookingsInVenueAsync(venueId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<int>());

            // Act
            var result = await _service.GetUsersWithoutBookingsAtVenueAsync(venueId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [TestMethod]
        public async Task GetUsersWithoutBookingsAtVenueAsync_PassesCancellationToken()
        {
            // Arrange
            var venueId = 1;
            var cancellationToken = new CancellationToken();

            _mockBookingRepository
                .Setup(x => x.FindUsersWithoutBookingsInVenueAsync(venueId, cancellationToken))
                .ReturnsAsync(new List<int>());

            // Act
            await _service.GetUsersWithoutBookingsAtVenueAsync(venueId, cancellationToken);

            // Assert
            _mockBookingRepository.Verify(
                x => x.FindUsersWithoutBookingsInVenueAsync(venueId, cancellationToken), 
                Times.Once);
        }

        #endregion

        #region DTO Mapping Tests

        [TestMethod]
        public async Task GetBookingsByUserIdAsync_WithBookingItems_MapsItemsCorrectly()
        {
            // Arrange
            var userId = 1;
            var user = new User { Id = userId, Name = "Test User", Email = "test@example.com" };
            var venue = new Venue { Id = 1, Name = "Stadium", Address = "Sports Ave" };
            var venueSeat = new VenueSeat { Id = 1, Row = "A", SeatNumber = "15", SeatLabel = "A15" };
            var eventSeat = new EventSeat { Id = 1, VenueSeatId = 1, Status = SeatStatus.Reserved, VenueSeat = venueSeat };
            
            var evnt = new ReservedSeatingEvent
            {
                Id = 1,
                VenueId = 1,
                Name = "Theatre Show",
                StartsAt = DateTime.UtcNow.AddDays(7),
                Venue = venue,
                Seats = new List<EventSeat> { eventSeat }
            };

            var bookingItem = new BookingItem
            {
                Id = 1,
                EventSeat = eventSeat,
                Quantity = 1
            };

            var booking = new Booking
            {
                Id = 1,
                User = user,
                Event = evnt,
                BookingType = BookingType.Seat,
                PaymentStatus = PaymentStatus.Paid,
                TotalAmount = 85m,
                CreatedAt = DateTime.UtcNow,
                BookingItems = new List<BookingItem> { bookingItem }
            };

            _mockBookingRepository
                .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Booking> { booking });

            // Act
            var result = await _service.GetBookingsByUserIdAsync(userId);

            // Assert
            var bookingDto = result.First();
            bookingDto.BookingItems.Should().HaveCount(1);
            bookingDto.BookingItems.First().EventSeatId.Should().Be(1);
            bookingDto.BookingItems.First().SeatLabel.Should().Be("A15");
            bookingDto.BookingItems.First().Quantity.Should().Be(1);
        }

        #endregion
    }
}
