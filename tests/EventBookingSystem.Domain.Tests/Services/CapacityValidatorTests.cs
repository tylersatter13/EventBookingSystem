using AwesomeAssertions;
using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Domain.Services;
using EventBookingSystem.Domain.Tests.Helpers;

namespace EventBookingSystem.Domain.Tests.Services
{
    [TestClass]
    public class CapacityValidatorTests
    {
        [TestMethod]
        public void CapacityValidator_ShouldReturnSuccess_WhenCapacityIsLessThanMax()
        {
            // Arrange
            var venue = TestDataBuilder.CreateVenueWithCapacity("Test Venue", "123 Test St", 100);
            var evnt = new GeneralAdmissionEvent 
            { 
                Name = "Test Event",
                EstimatedAttendance = 80,
                Capacity = 100
            };
            var validator = new CapacityValidator();
            
            // Act
            var result = validator.Validate(venue, evnt);
            
            // Assert
            result.IsValid.Should().BeTrue();
        }
        
        [TestMethod]
        public void CapacityValidator_ShouldReturnFailure_WhenCapacityExceedsMax()
        {
            // Arrange
            var venue = TestDataBuilder.CreateVenueWithCapacity("Test Venue", "123 Test St", 100);
            var evnt = new SectionBasedEvent 
            { 
                Name = "Test Event",
                EstimatedAttendance = 120
            };
            var validator = new CapacityValidator();
            
            // Act
            var result = validator.Validate(venue, evnt);
            
            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Be("The event exceeds the venue's maximum capacity.");
        }
        
        [TestMethod]
        public void CapacityValidator_WorksWithAllEventTypes()
        {
            // Arrange
            var venue = TestDataBuilder.CreateVenueWithCapacity("Venue", "Address", 500);
            var validator = new CapacityValidator();
            
            var gaEvent = new GeneralAdmissionEvent { EstimatedAttendance = 400, Capacity = 500 };
            var sbEvent = new SectionBasedEvent { EstimatedAttendance = 450 };
            var rsEvent = TestDataBuilder.CreateReservedSeatingEvent(venue, "Play", DateTime.Now);
            rsEvent.EstimatedAttendance = 480;
            
            // Act & Assert
            validator.Validate(venue, gaEvent).IsValid.Should().BeTrue();
            validator.Validate(venue, sbEvent).IsValid.Should().BeTrue();
            validator.Validate(venue, rsEvent).IsValid.Should().BeTrue();
        }
    }
}
