using AwesomeAssertions;
using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Domain.Services;
using EventBookingSystem.Domain.Tests.Helpers;

namespace EventBookingSystem.Domain.Tests.Services
{
    [TestClass]
    public class EventBookingServiceTests
    {
        [TestMethod]
        public void EventBookingService_ShouldBookGeneralAdmissionEvent_WhenValidationIsSuccessful()
        {
            // Arrange
            var venue = TestDataBuilder.CreateVenueWithCapacity("City Arena", "456 Elm St", 5000);
            var evnt = new GeneralAdmissionEvent
            {
                Name = "Tech Conference",
                StartsAt = DateTime.Now.AddDays(10),
                EndsAt = DateTime.Now.AddDays(10).AddHours(8),
                EstimatedAttendance = 3000,
                Capacity = 5000
            };
            var capacityValidator = new CapacityValidator();
            var timeConflictValidator = new TimeConflictValidator();
            var bookingService = new EventBookingService(capacityValidator, timeConflictValidator);

            // Act
            bookingService.BookEvent(venue, evnt);

            // Assert
            venue.Events.Should().Contain(evnt);
        }
        
        [TestMethod]
        public void EventBookingService_ShouldBookSectionBasedEvent_WhenValidationIsSuccessful()
        {
            // Arrange
            var venue = TestDataBuilder.CreateVenueWithCapacity("Arena", "123 Main St", 3000);
            var evnt = new SectionBasedEvent
            {
                Name = "Rock Concert",
                StartsAt = DateTime.Now.AddDays(15),
                EndsAt = DateTime.Now.AddDays(15).AddHours(4),
                EstimatedAttendance = 2500,
                SectionInventories = new List<EventSectionInventory>
                {
                    new EventSectionInventory { VenueSectionId = 1, Capacity = 2000, Price = 100m },
                    new EventSectionInventory { VenueSectionId = 2, Capacity = 1000, Price = 60m }
                }
            };
            var capacityValidator = new CapacityValidator();
            var timeConflictValidator = new TimeConflictValidator();
            var bookingService = new EventBookingService(capacityValidator, timeConflictValidator);

            // Act
            bookingService.BookEvent(venue, evnt);

            // Assert
            venue.Events.Should().Contain(evnt);
        }
        
        [TestMethod]
        public void EventBookingService_ShouldBookReservedSeatingEvent_WhenValidationIsSuccessful()
        {
            // Arrange
            var venue = TestDataBuilder.CreateVenueWithCapacity("Theatre", "789 Broadway", 500);
            var evnt = TestDataBuilder.CreateReservedSeatingEvent(venue, "Hamilton", DateTime.Now.AddMonths(1));
            evnt.EstimatedAttendance = 450;
            
            var capacityValidator = new CapacityValidator();
            var timeConflictValidator = new TimeConflictValidator();
            var bookingService = new EventBookingService(capacityValidator, timeConflictValidator);

            // Act
            bookingService.BookEvent(venue, evnt);

            // Assert
            venue.Events.Should().Contain(evnt);
        }
        
        [TestMethod]
        public void EventBookingService_ShouldThrowException_WhenCapacityExceeded()
        {
            // Arrange
            var venue = TestDataBuilder.CreateVenueWithCapacity("City Arena", "456 Elm St", 2000);
            var evnt = new GeneralAdmissionEvent
            {
                Name = "Large Festival",
                StartsAt = DateTime.Now.AddDays(5),
                EndsAt = DateTime.Now.AddDays(5).AddHours(10),
                EstimatedAttendance = 3000,  // Exceeds capacity
                Capacity = 3000
            };
            var capacityValidator = new CapacityValidator();
            var timeConflictValidator = new TimeConflictValidator();
            var bookingService = new EventBookingService(capacityValidator, timeConflictValidator);
           
            // Act
            Action act = () => bookingService.BookEvent(venue, evnt);
            
            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("The event exceeds the venue's maximum capacity.");
        }
        
        [TestMethod]
        public void EventBookingService_ShouldThrowException_WhenTimeConflictExists()
        {
            // Arrange
            var venue = TestDataBuilder.CreateVenueWithCapacity("Theatre", "123 Main St", 500);
            
            var existingEvent = new GeneralAdmissionEvent
            {
                Name = "Evening Show",
                StartsAt = DateTime.Now.AddDays(7).AddHours(19),
                EndsAt = DateTime.Now.AddDays(7).AddHours(21),
                EstimatedAttendance = 400,
                Capacity = 500
            };
            venue.BookEvent(existingEvent);
            
            var conflictingEvent = new GeneralAdmissionEvent
            {
                Name = "Late Night Show",
                StartsAt = DateTime.Now.AddDays(7).AddHours(20),  // Overlaps with existing
                EndsAt = DateTime.Now.AddDays(7).AddHours(22),
                EstimatedAttendance = 300,
                Capacity = 500
            };
            
            var capacityValidator = new CapacityValidator();
            var timeConflictValidator = new TimeConflictValidator();
            var bookingService = new EventBookingService(capacityValidator, timeConflictValidator);
           
            // Act
            Action act = () => bookingService.BookEvent(venue, conflictingEvent);
            
            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("The event conflicts with existing scheduled events at the venue.");
        }
        
        [TestMethod]
        public void ValidateBooking_WithValidEvent_ReturnsSuccess()
        {
            // Arrange
            var venue = TestDataBuilder.CreateVenueWithCapacity("Venue", "Address", 1000);
            var evnt = new GeneralAdmissionEvent
            {
                Name = "Event",
                StartsAt = DateTime.Now.AddDays(10),
                EstimatedAttendance = 500,
                Capacity = 1000
            };
            var capacityValidator = new CapacityValidator();
            var timeConflictValidator = new TimeConflictValidator();
            var bookingService = new EventBookingService(capacityValidator, timeConflictValidator);

            // Act
            var result = bookingService.ValidateBooking(venue, evnt);

            // Assert
            result.IsValid.Should().BeTrue();
        }
        
        [TestMethod]
        public void ValidateBooking_WithInvalidEvent_ReturnsFailure()
        {
            // Arrange
            var venue = TestDataBuilder.CreateVenueWithCapacity("Small Venue", "Address", 100);
            var evnt = new GeneralAdmissionEvent
            {
                Name = "Big Event",
                StartsAt = DateTime.Now.AddDays(10),
                EstimatedAttendance = 500,  // Exceeds capacity
                Capacity = 500
            };
            var capacityValidator = new CapacityValidator();
            var timeConflictValidator = new TimeConflictValidator();
            var bookingService = new EventBookingService(capacityValidator, timeConflictValidator);

            // Act
            var result = bookingService.ValidateBooking(venue, evnt);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Contain("exceeds the venue's maximum capacity");
        }
    }
}
