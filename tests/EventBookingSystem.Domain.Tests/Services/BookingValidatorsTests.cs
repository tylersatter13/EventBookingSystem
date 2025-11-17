using AwesomeAssertions;
using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Domain.Services;
using EventBookingSystem.Domain.Tests.Helpers;

namespace EventBookingSystem.Domain.Tests.Services
{
    [TestClass]
    public class BookingValidatorsTests
    {
        [TestClass]
        public class UserBookingLimitValidatorTests
        {
            [TestMethod]
            public void Validate_UnderLimit_ReturnsSuccess()
            {
                // Arrange
                var validator = new UserBookingLimitValidator();
                var venue = TestDataBuilder.CreateVenueWithCapacity("Venue", "Address", 1000);
                var evnt = new GeneralAdmissionEvent
                {
                    Id = 1,
                    Name = "Event",
                    Capacity = 1000,
                    Venue = venue
                };
                
                var user = new User
                {
                    Id = 1,
                    Name = "John Doe",
                    Email = "john@example.com",
                    Bookings = new List<Booking>()
                };
                
                var request = new ReservationRequest { Quantity = 2 };

                // Act
                var result = validator.Validate(user, evnt, request);

                // Assert
                result.IsValid.Should().BeTrue();
            }

            [TestMethod]
            public void Validate_ExceedsLimit_ReturnsFailure()
            {
                // Arrange
                var validator = new UserBookingLimitValidator();
                var venue = TestDataBuilder.CreateVenueWithCapacity("Venue", "Address", 1000);
                var evnt = new GeneralAdmissionEvent
                {
                    Id = 1,
                    Name = "Event",
                    Capacity = 1000,
                    Venue = venue
                };
                
                // User already has 3 tickets
                var existingBooking = new Booking
                {
                    Event = evnt,
                    PaymentStatus = PaymentStatus.Paid,
                    BookingItems = new List<BookingItem>
                    {
                        new() { Quantity = 3 }
                    }
                };
                
                var user = new User
                {
                    Id = 1,
                    Name = "John Doe",
                    Email = "john@example.com",
                    Bookings = new List<Booking> { existingBooking }
                };
                
                var request = new ReservationRequest { Quantity = 2 }; // Would be 5 total

                // Act
                var result = validator.Validate(user, evnt, request);

                // Assert
                result.IsValid.Should().BeFalse();
                result.ErrorMessage.Should().Contain("Booking limit exceeded");
                result.ErrorMessage.Should().Contain("already have 3");
            }

            [TestMethod]
            public void Validate_IgnoresRefundedBookings()
            {
                // Arrange
                var validator = new UserBookingLimitValidator();
                var venue = TestDataBuilder.CreateVenueWithCapacity("Venue", "Address", 1000);
                var evnt = new GeneralAdmissionEvent
                {
                    Id = 1,
                    Name = "Event",
                    Capacity = 1000,
                    Venue = venue
                };
                
                // User has refunded booking - should not count
                var refundedBooking = new Booking
                {
                    Event = evnt,
                    PaymentStatus = PaymentStatus.Refunded,
                    BookingItems = new List<BookingItem>
                    {
                        new() { Quantity = 5 }
                    }
                };
                
                var user = new User
                {
                    Id = 1,
                    Name = "Jane Smith",
                    Email = "jane@example.com",
                    Bookings = new List<Booking> { refundedBooking }
                };
                
                var request = new ReservationRequest { Quantity = 4 };

                // Act
                var result = validator.Validate(user, evnt, request);

                // Assert
                result.IsValid.Should().BeTrue(because: "refunded bookings should not count toward limit");
            }
        }

        [TestClass]
        public class EventAvailabilityValidatorTests
        {
            [TestMethod]
            public void Validate_FutureEvent_ReturnsSuccess()
            {
                // Arrange
                var validator = new EventAvailabilityValidator();
                var venue = TestDataBuilder.CreateVenueWithCapacity("Venue", "Address", 1000);
                var evnt = new GeneralAdmissionEvent
                {
                    Name = "Future Event",
                    StartsAt = DateTime.UtcNow.AddDays(30),
                    Capacity = 1000,
                    Venue = venue
                };
                
                var user = new User { Id = 1, Name = "User", Email = "user@example.com" };
                var request = new ReservationRequest { Quantity = 2 };

                // Act
                var result = validator.Validate(user, evnt, request);

                // Assert
                result.IsValid.Should().BeTrue();
            }

            [TestMethod]
            public void Validate_EventStarted_ReturnsFailure()
            {
                // Arrange
                var validator = new EventAvailabilityValidator();
                var venue = TestDataBuilder.CreateVenueWithCapacity("Venue", "Address", 1000);
                var evnt = new GeneralAdmissionEvent
                {
                    Name = "Started Event",
                    StartsAt = DateTime.UtcNow.AddHours(-1), // Already started
                    Capacity = 1000,
                    Venue = venue
                };
                
                var user = new User { Id = 1, Name = "User", Email = "user@example.com" };
                var request = new ReservationRequest { Quantity = 2 };

                // Act
                var result = validator.Validate(user, evnt, request);

                // Assert
                result.IsValid.Should().BeFalse();
                result.ErrorMessage.Should().Contain("already started");
            }

            [TestMethod]
            public void Validate_SoldOutEvent_ReturnsFailure()
            {
                // Arrange
                var validator = new EventAvailabilityValidator();
                var venue = TestDataBuilder.CreateVenueWithCapacity("Venue", "Address", 100);
                var evnt = new GeneralAdmissionEvent
                {
                    Name = "Sold Out Event",
                    StartsAt = DateTime.UtcNow.AddDays(10),
                    Capacity = 100,
                    Venue = venue
                };
                evnt.ReserveTickets(100); // Sell out
                
                var user = new User { Id = 1, Name = "User", Email = "user@example.com" };
                var request = new ReservationRequest { Quantity = 1 };

                // Act
                var result = validator.Validate(user, evnt, request);

                // Assert
                result.IsValid.Should().BeFalse();
                result.ErrorMessage.Should().Contain("sold out");
            }
        }

        [TestClass]
        public class UserInformationValidatorTests
        {
            [TestMethod]
            public void Validate_CompleteUser_ReturnsSuccess()
            {
                // Arrange
                var validator = new UserInformationValidator();
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
                    Id = 1,
                    Name = "Complete User",
                    Email = "complete@example.com",
                    PhoneNumber = "555-1234"
                };
                
                var request = new ReservationRequest { Quantity = 1 };

                // Act
                var result = validator.Validate(user, evnt, request);

                // Assert
                result.IsValid.Should().BeTrue();
            }

            [TestMethod]
            public void Validate_NullUser_ReturnsFailure()
            {
                // Arrange
                var validator = new UserInformationValidator();
                var venue = TestDataBuilder.CreateVenueWithCapacity("Venue", "Address", 1000);
                var evnt = new GeneralAdmissionEvent
                {
                    Name = "Event",
                    Capacity = 1000,
                    Venue = venue
                };
                
                var request = new ReservationRequest { Quantity = 1 };

                // Act
                var result = validator.Validate(null, evnt, request);

                // Assert
                result.IsValid.Should().BeFalse();
                result.ErrorMessage.Should().Contain("User information is required");
            }

            [TestMethod]
            public void Validate_MissingEmail_ReturnsFailure()
            {
                // Arrange
                var validator = new UserInformationValidator();
                var venue = TestDataBuilder.CreateVenueWithCapacity("Venue", "Address", 1000);
                var evnt = new GeneralAdmissionEvent
                {
                    Name = "Event",
                    Capacity = 1000,
                    Venue = venue
                };
                
                var user = new User
                {
                    Id = 1,
                    Name = "No Email User",
                    Email = "" // Empty email
                };
                
                var request = new ReservationRequest { Quantity = 1 };

                // Act
                var result = validator.Validate(user, evnt, request);

                // Assert
                result.IsValid.Should().BeFalse();
                result.ErrorMessage.Should().Contain("email is required");
            }

            [TestMethod]
            public void Validate_MissingName_ReturnsFailure()
            {
                // Arrange
                var validator = new UserInformationValidator();
                var venue = TestDataBuilder.CreateVenueWithCapacity("Venue", "Address", 1000);
                var evnt = new GeneralAdmissionEvent
                {
                    Name = "Event",
                    Capacity = 1000,
                    Venue = venue
                };
                
                var user = new User
                {
                    Id = 1,
                    Name = null, // Missing name
                    Email = "noname@example.com"
                };
                
                var request = new ReservationRequest { Quantity = 1 };

                // Act
                var result = validator.Validate(user, evnt, request);

                // Assert
                result.IsValid.Should().BeFalse();
                result.ErrorMessage.Should().Contain("name is required");
            }
        }

        [TestClass]
        public class BookingQuantityValidatorTests
        {
            [TestMethod]
            public void Validate_ValidQuantity_ReturnsSuccess()
            {
                // Arrange
                var validator = new BookingQuantityValidator();
                var venue = TestDataBuilder.CreateVenueWithCapacity("Venue", "Address", 1000);
                var evnt = new GeneralAdmissionEvent
                {
                    Name = "Event",
                    Capacity = 1000,
                    Venue = venue
                };
                
                var user = new User { Id = 1, Name = "User", Email = "user@example.com" };
                var request = new ReservationRequest { Quantity = 5 };

                // Act
                var result = validator.Validate(user, evnt, request);

                // Assert
                result.IsValid.Should().BeTrue();
            }

            [TestMethod]
            public void Validate_QuantityTooLow_ReturnsFailure()
            {
                // Arrange
                var validator = new BookingQuantityValidator();
                var venue = TestDataBuilder.CreateVenueWithCapacity("Venue", "Address", 1000);
                var evnt = new GeneralAdmissionEvent
                {
                    Name = "Event",
                    Capacity = 1000,
                    Venue = venue
                };
                
                var user = new User { Id = 1, Name = "User", Email = "user@example.com" };
                var request = new ReservationRequest { Quantity = 0 };

                // Act
                var result = validator.Validate(user, evnt, request);

                // Assert
                result.IsValid.Should().BeFalse();
                result.ErrorMessage.Should().Contain("Minimum booking quantity is 1");
            }

            [TestMethod]
            public void Validate_QuantityTooHigh_ReturnsFailure()
            {
                // Arrange
                var validator = new BookingQuantityValidator();
                var venue = TestDataBuilder.CreateVenueWithCapacity("Venue", "Address", 1000);
                var evnt = new GeneralAdmissionEvent
                {
                    Name = "Event",
                    Capacity = 1000,
                    Venue = venue
                };
                
                var user = new User { Id = 1, Name = "User", Email = "user@example.com" };
                var request = new ReservationRequest { Quantity = 15 };

                // Act
                var result = validator.Validate(user, evnt, request);

                // Assert
                result.IsValid.Should().BeFalse();
                result.ErrorMessage.Should().Contain("Maximum booking quantity is 10");
            }
        }
    }
}
