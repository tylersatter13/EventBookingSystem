using AwesomeAssertions;
using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Domain.Services;

namespace EventBookingSystem.Domain.Tests.Services
{
    [TestClass]
    public class TimeConflicValidatorTests
    {
        [TestMethod]
        public void TimeConflictValidator_ShouldDetectConflict_WhenTimesOverlap()
        {
            // Arrange
            var venue = new Venue() { 
                Address = "456 Broadway",
                MaxCapacity = 500,
                Name = "City Theater" };
            var existingEvent = new Event()
            {
                Name = "Evening Play",
                StartsAt = DateTime.Now.AddDays(2).AddHours(18),
                EndsAt = DateTime.Now.AddDays(2).AddHours(20),
                EstimatedAttendance = 300
            };
            venue.BookEvent(existingEvent);
            var newEvent = new Event()
            {
                Name = "Late Night Show",
                StartsAt = DateTime.Now.AddDays(2).AddHours(19), // Overlaps with existing event
                EndsAt = DateTime.Now.AddDays(2).AddHours(21),
                EstimatedAttendance = 200
            };
            var validator = new TimeConflictValidator();

            // Act
            var result = validator.Validate(venue, newEvent);
            
            // Assert
            result.IsValid.Should().BeFalse();

        }
        [TestMethod]
        public void TimeConflictValidator_ShouldNotDetectConflict_WhenTimesDoNotOverlap()
        {
            // Arrange
            var venue = new Venue()
            {
                Address = "789 Market St",
                MaxCapacity = 800,
                Name = "Downtown Arena"
            };
            var existingEvent = new Event()
            {
                Name = "Morning Yoga",
                StartsAt = DateTime.Now.AddDays(3).AddHours(8),
                EndsAt = DateTime.Now.AddDays(3).AddHours(9),
                EstimatedAttendance = 100
            };
            venue.BookEvent(existingEvent);
            var newEvent = new Event()
            {
                Name = "Afternoon Workshop",
                StartsAt = DateTime.Now.AddDays(3).AddHours(10), // Does not overlap
                EndsAt = DateTime.Now.AddDays(3).AddHours(12),
                EstimatedAttendance = 150
            };
            var validator = new TimeConflictValidator();
            // Act
            var result = validator.Validate(venue, newEvent);
            // Assert
            result.IsValid.Should().BeTrue();
        }

    }
}
