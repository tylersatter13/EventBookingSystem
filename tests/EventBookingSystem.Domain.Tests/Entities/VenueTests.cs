using AwesomeAssertions;
using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Domain.Tests.Helpers;

namespace EventBookingSystem.Domain.Tests.Entities
{
    [TestClass]
    public class VenueTests
    {
        [TestMethod]
        public void MaxCapacity_WithSections_CalculatesCorrectly()
        {
            // Arrange
            var venue = TestDataBuilder.CreateVenueWithCapacity("Grand Hall", "123 Main St", 1000);
            
            // Act
            var capacity = venue.MaxCapacity;
            
            // Assert
            capacity.Should().Be(1000);
        }
        
        [TestMethod]
        public void MaxCapacity_WithMultipleSections_ReturnsSumOfAllSections()
        {
            // Arrange
            var venue = TestDataBuilder.CreateVenueWithSections(
                "Grand Hall", 
                "123 Main St",
                ("Orchestra", 500),
                ("Balcony", 300),
                ("VIP", 100)
            );
            
            // Act
            var capacity = venue.MaxCapacity;
            
            // Assert
            capacity.Should().Be(900);
        }
        
        [TestMethod]
        public void TotalSeats_ReturnsCorrectCount()
        {
            // Arrange
            var venue = TestDataBuilder.CreateVenueWithSections(
                "Theatre",
                "456 Theatre Ln",
                ("Main Floor", 200),
                ("Upper Deck", 150)
            );
            
            // Act
            var totalSeats = venue.TotalSeats;
            
            // Assert
            totalSeats.Should().Be(350);
        }
        
        [TestMethod]
        public void BookEvent_AddsGeneralAdmissionEventToVenue()
        {
            // Arrange
            var venue = TestDataBuilder.CreateVenueWithCapacity("Grand Hall", "123 Main St", 1000);
            var evnt = new GeneralAdmissionEvent
            {
                Name = "Big Concert",
                StartsAt = DateTime.Now.AddDays(1),
                EndsAt = DateTime.Now.AddDays(1).AddHours(3),
                EstimatedAttendance = 500,
                Capacity = 1000,
                Venue = venue
            };
            
            // Act
            venue.BookEvent(evnt);
            
            // Assert
            venue.Events.Should().Contain(evnt);
        }
        
        [TestMethod]
        public void GetGeneralAdmissionEvents_ReturnsOnlyGAEvents()
        {
            // Arrange
            var venue = TestDataBuilder.CreateVenueWithCapacity("Multi-Purpose Hall", "789 Hall St", 1000);
            
            var gaEvent = new GeneralAdmissionEvent
            {
                Name = "GA Concert",
                StartsAt = DateTime.Now.AddDays(1),
                Capacity = 1000
            };
            
            var sbEvent = new SectionBasedEvent
            {
                Name = "Section Event",
                StartsAt = DateTime.Now.AddDays(2),
                SectionInventories = new List<EventSectionInventory>()
            };
            
            venue.BookEvent(gaEvent);
            venue.BookEvent(sbEvent);
            
            // Act
            var gaEvents = venue.GetGeneralAdmissionEvents().ToList();
            
            // Assert
            gaEvents.Should().HaveCount(1);
            gaEvents.Should().Contain(gaEvent);
        }
        
        [TestMethod]
        public void GetSectionBasedEvents_ReturnsOnlySectionEvents()
        {
            // Arrange
            var venue = TestDataBuilder.CreateVenueWithCapacity("Arena", "456 Arena Blvd", 5000);
            
            var sbEvent1 = new SectionBasedEvent
            {
                Name = "Concert",
                StartsAt = DateTime.Now.AddDays(1),
                SectionInventories = new List<EventSectionInventory>()
            };
            
            var gaEvent = new GeneralAdmissionEvent
            {
                Name = "Festival",
                StartsAt = DateTime.Now.AddDays(2),
                Capacity = 5000
            };
            
            venue.BookEvent(sbEvent1);
            venue.BookEvent(gaEvent);
            
            // Act
            var sectionEvents = venue.GetSectionBasedEvents().ToList();
            
            // Assert
            sectionEvents.Should().HaveCount(1);
            sectionEvents.Should().Contain(sbEvent1);
        }
    }
}
