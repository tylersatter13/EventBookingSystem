using AwesomeAssertions;
using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Domain.Services;

namespace EventBookingSystem.Domain.Tests.Services
{
    [TestClass]
    public class GeneralAdmissionStrategyTests
    {
        [TestMethod]
        public void CanHandle_EventTypeGeneralAdmission_ReturnsTrue()
        {
            // Arrange
            var strategy = new GeneralAdmissionStrategy();
            var eventType = EventType.GeneralAdmission;

            // Act
            var result = strategy.CanHandle(eventType);

            // Assert
            result.Should().BeTrue();
        }
        [TestMethod]
        public void CanHandle_EventTypeReservedSeating_ReturnsFalse()
        {
            // Arrange
            var strategy = new GeneralAdmissionStrategy();
            var eventType = EventType.Reserved;

            // Act
            var result = strategy.CanHandle(eventType);

            // Assert
            result.Should().BeFalse();
        }
        [TestMethod]
        public void CanHandle_EventTypeMixed_ReturnsFalse()
        {
            // Arrange
            var strategy = new GeneralAdmissionStrategy();
            var eventType = EventType.Mixed;
            // Act
            var result = strategy.CanHandle(eventType);
            // Assert
            result.Should().BeFalse();
        }
        [TestMethod]
        public void ValidateReservation_WithCapacity_ReturnsIsValid()
        {
            // Arrange
            var strategy = new GeneralAdmissionStrategy();
            var venue = new Venue {
                Name = "The Garden",
                Address = "123 Main St",
                MaxCapacity = 100 };
            
            var evnt = new Event { SeatsReservered = 80, EventType = EventType.GeneralAdmission };
            // Act
            var result = strategy.ValidateReservation(venue, evnt);
            // Assert
            result.IsValid.Should().BeTrue();
        }
        [TestMethod]
        public void ValidateReservation_ExceedingCapacity_ReturnsIsInvalid()
        {
            // Arrange
            var strategy = new GeneralAdmissionStrategy();
            var venue = new Venue
            {
                Name = "The Garden",
                Address = "123 Main St",
                MaxCapacity = 100
            };
            var evnt = new Event { SeatsReservered = 120, EventType = EventType.GeneralAdmission };
            // Act
            var result = strategy.ValidateReservation(venue, evnt);
            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Be("No available seats for this event.");
        }
        [TestMethod]
        public void ValidateReservation_AtCapacity_ReturnsIsInvalid()
        {
            // Arrange
            var strategy = new GeneralAdmissionStrategy();
            var venue = new Venue
            {
                Name = "The Garden",
                Address = "123 Main St",
                MaxCapacity = 100
            };
            var evnt = new Event { SeatsReservered = 100, EventType = EventType.GeneralAdmission };
            // Act
            var result = strategy.ValidateReservation(venue, evnt);
            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Be("No available seats for this event.");
        }
        [TestMethod]
        public void Reserve_WithinCapacity_IncrementsSeatsReserved()
        {
            // Arrange
            var strategy = new GeneralAdmissionStrategy();
            var venue = new Venue
            {
                Name = "The Garden",
                Address = "123 Main St",
                MaxCapacity = 100
            };
            var evnt = new Event { SeatsReservered = 50, EventType = EventType.GeneralAdmission };
            
            // Act
            strategy.Reserve(venue,evnt);
            
            // Assert
            evnt.SeatsReservered.Should().Be(51);
        }
        [TestMethod]
        public void Reserve_AtCapacity_ThrowsInvalidOperationException()
        {
            // Arrange
            var strategy = new GeneralAdmissionStrategy();
            var venue = new Venue
            {
                Name = "The Garden",
                Address = "123 Main St",
                MaxCapacity = 100
            };
            var evnt = new Event { SeatsReservered = 100, EventType = EventType.GeneralAdmission };
            
            // Act
            Action act = () => strategy.Reserve(venue, evnt);
            
            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("No available seats for this event.");
        }
        [TestMethod]    
        public void Reserve_ExceedingCapacity_ThrowsInvalidOperationException()
        {
            // Arrange
            var strategy = new GeneralAdmissionStrategy();
            var venue = new Venue
            {
                Name = "The Garden",
                Address = "123 Main St",
                MaxCapacity = 100
            };
            var evnt = new Event { SeatsReservered = 120, EventType = EventType.GeneralAdmission };
            
            // Act
            Action act = () => strategy.Reserve(venue, evnt);
            
            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("No available seats for this event.");
        }
    }
}
