using AwesomeAssertions;
using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Domain.Services;
using EventBookingSystem.Domain.Tests.Helpers;

namespace EventBookingSystem.Domain.Tests.Services
{
    [TestClass]
    public class BookingServiceTests
    {
        [TestMethod]
        public void CreateBooking_GeneralAdmission_CreatesBookingSuccessfully()
        {
            // Arrange
            var reservationService = new EventReservationService();
            var bookingService = new BookingService(reservationService);
            
            var venue = TestDataBuilder.CreateVenueWithCapacity("Arena", "123 St", 1000);
            var evnt = new GeneralAdmissionEvent
            {
                Name = "Concert",
                StartsAt = DateTime.UtcNow.AddDays(30),
                Capacity = 1000,
                Price = 50m,
                Venue = venue
            };
            
            var user = new User
            {
                Id = 1,
                Name = "John Doe",
                Email = "john@example.com"
            };
            
            var request = new ReservationRequest { Quantity = 2 };

            // Act
            var booking = bookingService.CreateBooking(user, evnt, request);

            // Assert
            booking.Should().NotBeNull();
            booking.User.Should().Be(user);
            booking.Event.Should().Be(evnt);
            booking.BookingType.Should().Be(BookingType.GA);
            booking.TotalAmount.Should().Be(100m); // 2 * 50
            booking.PaymentStatus.Should().Be(PaymentStatus.Pending);
            booking.BookingItems.Should().HaveCount(2);
        }

        [TestMethod]
        public void CreateBooking_SectionBased_CreatesBookingSuccessfully()
        {
            // Arrange
            var reservationService = new EventReservationService();
            var bookingService = new BookingService(reservationService);
            
            var venue = TestDataBuilder.CreateVenueWithCapacity("Stadium", "456 Ave", 5000);
            var evnt = new SectionBasedEvent
            {
                Name = "Game",
                StartsAt = DateTime.UtcNow.AddDays(15),
                Venue = venue,
                SectionInventories = new List<EventSectionInventory>
                {
                    new() 
                    { 
                        VenueSectionId = 1, 
                        Capacity = 500, 
                        Price = 100m,
                        VenueSection = new VenueSection { Id = 1, Name = "VIP" }
                    }
                }
            };
            
            var user = new User
            {
                Id = 2,
                Name = "Jane Smith",
                Email = "jane@example.com"
            };
            
            var request = new ReservationRequest 
            { 
                SectionId = 1, 
                Quantity = 3 
            };

            // Act
            var booking = bookingService.CreateBooking(user, evnt, request);

            // Assert
            booking.Should().NotBeNull();
            booking.BookingType.Should().Be(BookingType.Section);
            booking.TotalAmount.Should().Be(300m); // 3 * 100
            booking.BookingItems.Should().HaveCount(1);
            booking.BookingItems.First().Quantity.Should().Be(3);
        }

        [TestMethod]
        public void CreateBooking_ReservedSeating_CreatesBookingSuccessfully()
        {
            // Arrange
            var reservationService = new EventReservationService();
            var bookingService = new BookingService(reservationService);
            
            var venue = TestDataBuilder.CreateVenueWithCapacity("Theatre", "789 Blvd", 200);
            var evnt = TestDataBuilder.CreateReservedSeatingEvent(venue, "Play", DateTime.UtcNow.AddMonths(1));
            
            var user = new User
            {
                Id = 3,
                Name = "Bob Johnson",
                Email = "bob@example.com"
            };
            
            var request = new ReservationRequest 
            { 
                SeatId = evnt.Seats.First().VenueSeatId 
            };

            // Act
            var booking = bookingService.CreateBooking(user, evnt, request);

            // Assert
            booking.Should().NotBeNull();
            booking.BookingType.Should().Be(BookingType.Seat);
            booking.TotalAmount.Should().Be(0m); // Pricing not implemented yet
            booking.BookingItems.Should().HaveCount(1);
        }

        [TestMethod]
        public void CreateBooking_EventSoldOut_ThrowsException()
        {
            // Arrange
            var reservationService = new EventReservationService();
            var bookingService = new BookingService(reservationService);
            
            var venue = TestDataBuilder.CreateVenueWithCapacity("Small Club", "321 St", 50);
            var evnt = new GeneralAdmissionEvent
            {
                Name = "Sold Out Show",
                StartsAt = DateTime.UtcNow.AddDays(10),
                Capacity = 50,
                Price = 25m,
                Venue = venue
            };
            evnt.ReserveTickets(50); // Sell out
            
            var user = new User
            {
                Id = 4,
                Name = "Alice Brown",
                Email = "alice@example.com"
            };
            
            var request = new ReservationRequest { Quantity = 1 };

            // Act
            Action act = () => bookingService.CreateBooking(user, evnt, request);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*sold out*");
        }

        [TestMethod]
        public void ValidateBooking_ValidRequest_ReturnsSuccess()
        {
            // Arrange
            var reservationService = new EventReservationService();
            var bookingService = new BookingService(reservationService);
            
            var venue = TestDataBuilder.CreateVenueWithCapacity("Venue", "Address", 1000);
            var evnt = new GeneralAdmissionEvent
            {
                Name = "Event",
                StartsAt = DateTime.UtcNow.AddDays(30),
                Capacity = 1000,
                Venue = venue
            };
            
            var user = new User
            {
                Id = 5,
                Name = "Charlie Davis",
                Email = "charlie@example.com"
            };
            
            var request = new ReservationRequest { Quantity = 2 };

            // Act
            var result = bookingService.ValidateBooking(user, evnt, request);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void ValidateBooking_InsufficientCapacity_ReturnsFailure()
        {
            // Arrange
            var reservationService = new EventReservationService();
            var bookingService = new BookingService(reservationService);
            
            var venue = TestDataBuilder.CreateVenueWithCapacity("Small Venue", "Address", 100);
            var evnt = new GeneralAdmissionEvent
            {
                Name = "Almost Full Event",
                StartsAt = DateTime.UtcNow.AddDays(20),
                Capacity = 100,
                Venue = venue
            };
            evnt.ReserveTickets(95);
            
            var user = new User
            {
                Id = 6,
                Name = "Diana Evans",
                Email = "diana@example.com"
            };
            
            var request = new ReservationRequest { Quantity = 10 };

            // Act
            var result = bookingService.ValidateBooking(user, evnt, request);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Insufficient capacity");
        }

        [TestMethod]
        public void ValidateBooking_SectionBasedWithoutSectionId_ReturnsFailure()
        {
            // Arrange
            var reservationService = new EventReservationService();
            var bookingService = new BookingService(reservationService);
            
            var venue = TestDataBuilder.CreateVenueWithCapacity("Arena", "Address", 1000);
            var evnt = new SectionBasedEvent
            {
                Name = "Concert",
                StartsAt = DateTime.UtcNow.AddDays(25),
                Venue = venue,
                SectionInventories = new List<EventSectionInventory>
                {
                    new() { VenueSectionId = 1, Capacity = 1000 }
                }
            };
            
            var user = new User
            {
                Id = 7,
                Name = "Eve Foster",
                Email = "eve@example.com"
            };
            
            var request = new ReservationRequest { Quantity = 2 }; // Missing SectionId

            // Act
            var result = bookingService.ValidateBooking(user, evnt, request);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Section ID is required");
        }

        [TestMethod]
        public void ValidateBooking_WithCustomValidators_AppliesAllValidators()
        {
            // Arrange
            var reservationService = new EventReservationService();
            var validators = new List<IBookingValidator>
            {
                new UserInformationValidator(),
                new BookingQuantityValidator()
            };
            var bookingService = new BookingService(reservationService, validators);
            
            var venue = TestDataBuilder.CreateVenueWithCapacity("Venue", "Address", 1000);
            var evnt = new GeneralAdmissionEvent
            {
                Name = "Event",
                StartsAt = DateTime.UtcNow.AddDays(30),
                Capacity = 1000,
                Venue = venue
            };
            
            var user = new User
            {
                Id = 8,
                Name = "", // Invalid - empty name
                Email = "test@example.com"
            };
            
            var request = new ReservationRequest { Quantity = 2 };

            // Act
            var result = bookingService.ValidateBooking(user, evnt, request);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Contain("User name is required");
        }

        [TestMethod]
        public void CreateBooking_SetsCreatedAtToCurrentTime()
        {
            // Arrange
            var reservationService = new EventReservationService();
            var bookingService = new BookingService(reservationService);
            
            var venue = TestDataBuilder.CreateVenueWithCapacity("Venue", "Address", 500);
            var evnt = new GeneralAdmissionEvent
            {
                Name = "Event",
                StartsAt = DateTime.UtcNow.AddDays(30),
                Capacity = 500,
                Price = 40m,
                Venue = venue
            };
            
            var user = new User
            {
                Id = 9,
                Name = "Frank Green",
                Email = "frank@example.com"
            };
            
            var request = new ReservationRequest { Quantity = 1 };
            var beforeCreate = DateTime.UtcNow;

            // Act
            var booking = bookingService.CreateBooking(user, evnt, request);
            var afterCreate = DateTime.UtcNow;

            // Assert
            booking.CreatedAt.Should().BeAfter(beforeCreate.AddSeconds(-1));
            booking.CreatedAt.Should().BeBefore(afterCreate.AddSeconds(1));
        }
    }
}
