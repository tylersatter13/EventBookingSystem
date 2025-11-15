using AwesomeAssertions;
using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Domain.Services;

namespace EventBookingSystem.Domain.Tests.Services
{
    [TestClass]
    public class CapacityValidatorTests
    {
        [TestMethod]
        public void CapacityValidator_ShouldReturnSuccess_WhenCapacityIsLessThanMax()
        {
            // Arrange
            var venue = new Venue { 
                Name = "Test Venue",
                Address = "123 Test St",
                MaxCapacity = 100 
            };
            var evnt = new Event { EstimatedAttendance = 80 };
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
            var venue = new Venue { 
                Name = "Test Venue",
                Address = "123 Test St",
                MaxCapacity = 100 
            };
            var evnt = new Event { EstimatedAttendance = 120 };
            var validator = new CapacityValidator();
            // Act
            var result = validator.Validate(venue, evnt);
            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Be("The event exceeds the venue's maximum capacity.");
        }
    }
}
