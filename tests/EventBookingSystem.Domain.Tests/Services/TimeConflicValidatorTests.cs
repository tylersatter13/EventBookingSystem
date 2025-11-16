using AwesomeAssertions;
using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Domain.Services;
using EventBookingSystem.Domain.Tests.Helpers;

namespace EventBookingSystem.Domain.Tests.Services
{
    [TestClass]
    public class TimeConflicValidatorTests
    {
        [TestMethod]
        public void TimeConflictValidator_ShouldDetectConflict_WhenTimesOverlap()
        {
            // Arrange
            var venue = TestDataBuilder.CreateVenueWithCapacity("City Theater", "456 Broadway", 500);
            var existingEvent = new GeneralAdmissionEvent
            {
                Name = "Evening Play",
                StartsAt = DateTime.Now.AddDays(2).AddHours(18),
                EndsAt = DateTime.Now.AddDays(2).AddHours(20),
                EstimatedAttendance = 300,
                Capacity = 500
            };
            venue.BookEvent(existingEvent);
            
            var newEvent = new SectionBasedEvent
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
            var venue = TestDataBuilder.CreateVenueWithCapacity("Downtown Arena", "789 Market St", 800);
            var existingEvent = new GeneralAdmissionEvent
            {
                Name = "Morning Yoga",
                StartsAt = DateTime.Now.AddDays(3).AddHours(8),
                EndsAt = DateTime.Now.AddDays(3).AddHours(9),
                EstimatedAttendance = 100,
                Capacity = 800
            };
            venue.BookEvent(existingEvent);
            
            var newEvent = new ReservedSeatingEvent
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
        
        [TestMethod]
        public void TimeConflictValidator_WorksAcrossDifferentEventTypes()
        {
            // Arrange
            var venue = TestDataBuilder.CreateVenueWithCapacity("Multi-Purpose Hall", "123 Main St", 1000);
            
            // Book different types of events
            var gaEvent = new GeneralAdmissionEvent
            {
                Name = "Morning Concert",
                StartsAt = DateTime.Now.AddDays(5).AddHours(10),
                EndsAt = DateTime.Now.AddDays(5).AddHours(12),
                EstimatedAttendance = 800,
                Capacity = 1000
            };
            venue.BookEvent(gaEvent);
            
            var sbEvent = new SectionBasedEvent
            {
                Name = "Afternoon Conference",
                StartsAt = DateTime.Now.AddDays(5).AddHours(14),
                EndsAt = DateTime.Now.AddDays(5).AddHours(17),
                EstimatedAttendance = 600
            };
            venue.BookEvent(sbEvent);
            
            // Try to book conflicting event
            var conflictingEvent = new ReservedSeatingEvent
            {
                Name = "Overlapping Show",
                StartsAt = DateTime.Now.AddDays(5).AddHours(16),  // Conflicts with conference
                EndsAt = DateTime.Now.AddDays(5).AddHours(18),
                EstimatedAttendance = 500
            };
            
            var validator = new TimeConflictValidator();
            
            // Act
            var result = validator.Validate(venue, conflictingEvent);
            
            // Assert
            result.IsValid.Should().BeFalse();
        }
    }
}
