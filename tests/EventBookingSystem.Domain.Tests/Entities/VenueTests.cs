using AwesomeAssertions;
using EventBookingSystem.Domain.Entities;

namespace EventBookingSystem.Domain.Tests.Entities
{
    [TestClass]
    public class VenueTests
    {
        [TestMethod]
        public void Venue_ShouldRejectBooking_WhenCapacityExceeded()
        {
            // Arrange
            var venue = new Venue() { Address = "123 Main St", MaxCapacity = 1000, Name = "Grand Hall" };
            var evnt = new Event()
            {
                Name = "Big Concert",
                StartsAt = DateTime.Now.AddDays(1),
                EndsAt = DateTime.Now.AddDays(1).AddHours(3),
                EstimatedAttendance = 1500 // Exceeds venue capacity
            };
            // Act
            Action act = () => venue.BookEvent(evnt);
            
            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("The event exceeds the venue's maximum capacity.");
        }
        [TestMethod]
        public void Venue_ShouldAllowBooking_WhenCapacityNotExceeded()
        {
            // Arrange
            var venue = new Venue() { Address = "123 Main St", MaxCapacity = 1000, Name = "Grand Hall" };
            var evnt = new Event()
            {
                Name = "Small Concert",
                StartsAt = DateTime.Now.AddDays(1),
                EndsAt = DateTime.Now.AddDays(1).AddHours(2),
                EstimatedAttendance = 800 // Within venue capacity
            };
            // Act
            venue.BookEvent(evnt);
            
            // Assert
            venue.Events.Should().Contain(evnt);
        }
        [TestMethod]
        public void Venue_ShouldRejectBooking_WhenTimeConflictExists()
        {
            // Arrange
            var venue = new Venue() { Address = "123 Main St", MaxCapacity = 1000, Name = "Grand Hall" };
            var existingEvent = new Event()
            {
                Name = "Morning Show",
                StartsAt = DateTime.Now.AddDays(1).AddHours(9),
                EndsAt = DateTime.Now.AddDays(1).AddHours(11),
                EstimatedAttendance = 500
            };
            venue.BookEvent(existingEvent);
            var newEvent = new Event()
            {
                Name = "Overlapping Event",
                StartsAt = DateTime.Now.AddDays(1).AddHours(10), // Overlaps with existing event
                EndsAt = DateTime.Now.AddDays(1).AddHours(12),
                EstimatedAttendance = 300
            };
            // Act
            Action act = () => venue.BookEvent(newEvent);
            
            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("The event conflicts with existing scheduled events at the venue.");
        }
        [TestMethod]
        public void Venue_ShouldAllowBooking_WhenNoTimeConflictExists()
        {
            // Arrange
            var venue = new Venue() { Address = "123 Main St", MaxCapacity = 1000, Name = "Grand Hall" };
            var existingEvent = new Event()
            {
                Name = "Morning Show",
                StartsAt = DateTime.Now.AddDays(1).AddHours(9),
                EndsAt = DateTime.Now.AddDays(1).AddHours(11),
                EstimatedAttendance = 500
            };
            venue.BookEvent(existingEvent);
            var newEvent = new Event()
            {
                Name = "Afternoon Show",
                StartsAt = DateTime.Now.AddDays(1).AddHours(12), // No overlap
                EndsAt = DateTime.Now.AddDays(1).AddHours(14),
                EstimatedAttendance = 300
            };
            // Act
            venue.BookEvent(newEvent);
            
            // Assert
            venue.Events.Should().Contain(newEvent);
        }
    }
}
