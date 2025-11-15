using AwesomeAssertions;
using EventBookingSystem.Domain.Entities;

namespace EventBookingSystem.Domain.Tests.Entities
{
    [TestClass]
    public class VenueTests
    {
        [TestMethod]
        public void Venue_Should_BookEventSuccessfully()
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
      
    }
}
