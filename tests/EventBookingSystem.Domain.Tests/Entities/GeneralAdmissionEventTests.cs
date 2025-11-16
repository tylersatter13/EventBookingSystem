using AwesomeAssertions;
using EventBookingSystem.Domain.Entities;

namespace EventBookingSystem.Domain.Tests.Entities
{
    [TestClass]
    public class GeneralAdmissionEventTests
    {
        [TestMethod]
        public void TotalCapacity_WithNoOverride_ReturnsCapacity()
        {
            // Arrange
            var evnt = new GeneralAdmissionEvent
            {
                Name = "Festival",
                Capacity = 5000
            };

            // Act
            var capacity = evnt.TotalCapacity;

            // Assert
            capacity.Should().Be(5000);
        }

        [TestMethod]
        public void TotalCapacity_WithOverride_ReturnsOverride()
        {
            // Arrange
            var evnt = new GeneralAdmissionEvent
            {
                Name = "Festival with Stage",
                Capacity = 5000,
                CapacityOverride = 4500  // Reduced due to stage
            };

            // Act
            var capacity = evnt.TotalCapacity;

            // Assert
            capacity.Should().Be(4500, because: "override should take precedence");
        }

        [TestMethod]
        public void TotalReserved_InitiallyZero()
        {
            // Arrange
            var evnt = new GeneralAdmissionEvent
            {
                Name = "Concert",
                Capacity = 1000
            };

            // Act
            var reserved = evnt.TotalReserved;

            // Assert
            reserved.Should().Be(0);
            evnt.Attendees.Should().Be(0);
        }

        [TestMethod]
        public void IsSoldOut_WithAvailableCapacity_ReturnsFalse()
        {
            // Arrange
            var evnt = new GeneralAdmissionEvent
            {
                Name = "Show",
                Capacity = 500
            };
            evnt.ReserveTickets(250);

            // Act
            var isSoldOut = evnt.IsSoldOut;

            // Assert
            isSoldOut.Should().BeFalse();
        }

        [TestMethod]
        public void IsSoldOut_AtCapacity_ReturnsTrue()
        {
            // Arrange
            var evnt = new GeneralAdmissionEvent
            {
                Name = "Sold Out Show",
                Capacity = 100
            };
            evnt.ReserveTickets(100);

            // Act
            var isSoldOut = evnt.IsSoldOut;

            // Assert
            isSoldOut.Should().BeTrue();
        }

        [TestMethod]
        public void AvailableCapacity_CalculatesCorrectly()
        {
            // Arrange
            var evnt = new GeneralAdmissionEvent
            {
                Name = "Event",
                Capacity = 1000
            };
            evnt.ReserveTickets(350);

            // Act
            var available = evnt.AvailableCapacity;

            // Assert
            available.Should().Be(650);
        }

        [TestMethod]
        public void ReserveTickets_WithAvailableCapacity_IncrementsAttendees()
        {
            // Arrange
            var evnt = new GeneralAdmissionEvent
            {
                Name = "Concert",
                Capacity = 500
            };

            // Act
            evnt.ReserveTickets(50);

            // Assert
            evnt.Attendees.Should().Be(50);
            evnt.TotalReserved.Should().Be(50);
            evnt.AvailableCapacity.Should().Be(450);
        }

        [TestMethod]
        public void ReserveTickets_MultipleReservations_AccumulatesCorrectly()
        {
            // Arrange
            var evnt = new GeneralAdmissionEvent
            {
                Name = "Festival",
                Capacity = 10000
            };

            // Act
            evnt.ReserveTickets(1000);
            evnt.ReserveTickets(2000);
            evnt.ReserveTickets(500);

            // Assert
            evnt.Attendees.Should().Be(3500);
            evnt.AvailableCapacity.Should().Be(6500);
        }

        [TestMethod]
        public void ReserveTickets_ExceedingCapacity_ThrowsException()
        {
            // Arrange
            var evnt = new GeneralAdmissionEvent
            {
                Name = "Small Venue",
                Capacity = 100
            };

            // Act
            Action act = () => evnt.ReserveTickets(150);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Insufficient capacity*");
        }

        [TestMethod]
        public void ReserveTickets_AtCapacity_ThrowsException()
        {
            // Arrange
            var evnt = new GeneralAdmissionEvent
            {
                Name = "Sold Out",
                Capacity = 50
            };
            evnt.ReserveTickets(50);

            // Act
            Action act = () => evnt.ReserveTickets(1);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*sold out*");
        }

        [TestMethod]
        public void ReleaseTickets_WithReservedTickets_DecrementsAttendees()
        {
            // Arrange
            var evnt = new GeneralAdmissionEvent
            {
                Name = "Event",
                Capacity = 500
            };
            evnt.ReserveTickets(200);

            // Act
            evnt.ReleaseTickets(50);

            // Assert
            evnt.Attendees.Should().Be(150);
            evnt.AvailableCapacity.Should().Be(350);
        }

        [TestMethod]
        public void ReleaseTickets_MoreThanReserved_ThrowsException()
        {
            // Arrange
            var evnt = new GeneralAdmissionEvent
            {
                Name = "Event",
                Capacity = 500
            };
            evnt.ReserveTickets(100);

            // Act
            Action act = () => evnt.ReleaseTickets(150);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Cannot release 150 tickets. Only 100 are reserved*");
        }

        [TestMethod]
        public void ReleaseTickets_WithNegativeQuantity_ThrowsException()
        {
            // Arrange
            var evnt = new GeneralAdmissionEvent
            {
                Name = "Event",
                Capacity = 500
            };
            evnt.ReserveTickets(100);

            // Act
            Action act = () => evnt.ReleaseTickets(-10);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Quantity must be positive*");
        }

        [TestMethod]
        public void ValidateCapacity_WithAvailableCapacity_ReturnsSuccess()
        {
            // Arrange
            var evnt = new GeneralAdmissionEvent
            {
                Name = "Event",
                Capacity = 500
            };
            evnt.ReserveTickets(200);

            // Act
            var result = evnt.ValidateCapacity(100);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void ValidateCapacity_InsufficientCapacity_ReturnsFailure()
        {
            // Arrange
            var evnt = new GeneralAdmissionEvent
            {
                Name = "Event",
                Capacity = 100
            };
            evnt.ReserveTickets(90);

            // Act
            var result = evnt.ValidateCapacity(20);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Insufficient capacity");
            result.ErrorMessage.Should().Contain("Requested: 20");
            result.ErrorMessage.Should().Contain("Available: 10");
        }

        [TestMethod]
        public void ValidateCapacity_WhenSoldOut_ReturnsFailure()
        {
            // Arrange
            var evnt = new GeneralAdmissionEvent
            {
                Name = "Sold Out Event",
                Capacity = 50
            };
            evnt.ReserveTickets(50);

            // Act
            var result = evnt.ValidateCapacity(1);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Contain("sold out");
        }

        [TestMethod]
        public void Price_CanBeSet()
        {
            // Arrange
            var evnt = new GeneralAdmissionEvent
            {
                Name = "Paid Event",
                Capacity = 1000,
                Price = 35.50m
            };

            // Act
            var price = evnt.Price;

            // Assert
            price.Should().Be(35.50m);
        }
    }
}
