using AwesomeAssertions;
using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Domain.Services;

namespace EventBookingSystem.Domain.Tests.Services
{
    [TestClass]
    public class EventBookingServiceTests
    {
        [TestMethod]
        public void EventBookingService_ShouldBookEvent_WhenValidationIsSuccessful()
        {
            // Arrange
            var venue = new Venue() {
                Address = "456 Elm St",
                MaxCapacity = 5000,
                Name = "City Arena" };
            var evnt = new Event()
            {
                Name = "Tech Conference",
                StartsAt = DateTime.Now.AddDays(10),
                EndsAt = DateTime.Now.AddDays(10).AddHours(8),
                EstimatedAttendance = 3000
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
        public void EventBookingService_ShouldThrowException_WhenValidationFails()
        {
            // Arrange
            var venue = new Venue() {
                Address = "456 Elm St",
                MaxCapacity = 2000,
                Name = "City Arena" };
            var evnt = new Event()
            {
                Name = "Large Festival",
                StartsAt = DateTime.Now.AddDays(5),
                EndsAt = DateTime.Now.AddDays(5).AddHours(10),
                EstimatedAttendance = 3000 // Exceeds capacity
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
    }
}
