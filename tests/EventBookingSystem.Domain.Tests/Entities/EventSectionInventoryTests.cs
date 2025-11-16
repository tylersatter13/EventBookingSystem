using AwesomeAssertions;
using EventBookingSystem.Domain.Entities;

namespace EventBookingSystem.Domain.Tests.Entities
{
    [TestClass]
    public class EventSectionInventoryTests
    {
        [TestMethod]
        public void Remaining_WithNoBookings_ReturnsFullCapacity()
        {
            // Arrange
            var inventory = new EventSectionInventory
            {
                Capacity = 100
            };

            // Act
            var remaining = inventory.Remaining;

            // Assert
            remaining.Should().Be(100);
        }

        [TestMethod]
        public void IsSoldOut_WithRemainingCapacity_ReturnsFalse()
        {
            // Arrange
            var inventory = new EventSectionInventory
            {
                Capacity = 100
            };

            // Act
            var isSoldOut = inventory.IsSoldOut;

            // Assert
            isSoldOut.Should().BeFalse();
        }

        [TestMethod]
        public void IsSoldOut_AtCapacity_ReturnsTrue()
        {
            // Arrange
            var inventory = new EventSectionInventory
            {
                Capacity = 50
            };
            inventory.ReserveSeats(50);

            // Act
            var isSoldOut = inventory.IsSoldOut;

            // Assert
            isSoldOut.Should().BeTrue();
        }

        [TestMethod]
        public void ReserveSeats_WithAvailableCapacity_IncrementsBooked()
        {
            // Arrange
            var inventory = new EventSectionInventory
            {
                Capacity = 100
            };

            // Act
            inventory.ReserveSeats(25);

            // Assert
            inventory.Booked.Should().Be(25);
            inventory.Remaining.Should().Be(75);
        }

        [TestMethod]
        public void ReserveSeats_MultipleReservations_CorrectlyTracksBookings()
        {
            // Arrange
            var inventory = new EventSectionInventory
            {
                Capacity = 100
            };

            // Act
            inventory.ReserveSeats(20);
            inventory.ReserveSeats(30);
            inventory.ReserveSeats(15);

            // Assert
            inventory.Booked.Should().Be(65);
            inventory.Remaining.Should().Be(35);
        }

        [TestMethod]
        public void ReserveSeats_ExceedingCapacity_ThrowsInvalidOperationException()
        {
            // Arrange
            var inventory = new EventSectionInventory
            {
                Capacity = 50
            };

            // Act
            Action act = () => inventory.ReserveSeats(51);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Insufficient capacity in section. Requested: 51, Available: 50");
        }

        [TestMethod]
        public void ReserveSeats_WithNegativeQuantity_ThrowsArgumentException()
        {
            // Arrange
            var inventory = new EventSectionInventory
            {
                Capacity = 100
            };

            // Act
            Action act = () => inventory.ReserveSeats(-5);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Quantity must be positive*");
        }

        [TestMethod]
        public void ReserveSeats_WithZeroQuantity_ThrowsArgumentException()
        {
            // Arrange
            var inventory = new EventSectionInventory
            {
                Capacity = 100
            };

            // Act
            Action act = () => inventory.ReserveSeats(0);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Quantity must be positive*");
        }

        [TestMethod]
        public void ReleaseSeats_WithBookedSeats_DecrementsBooked()
        {
            // Arrange
            var inventory = new EventSectionInventory
            {
                Capacity = 100
            };
            inventory.ReserveSeats(50);

            // Act
            inventory.ReleaseSeats(20);

            // Assert
            inventory.Booked.Should().Be(30);
            inventory.Remaining.Should().Be(70);
        }

        [TestMethod]
        public void ReleaseSeats_MoreThanBooked_ThrowsInvalidOperationException()
        {
            // Arrange
            var inventory = new EventSectionInventory
            {
                Capacity = 100
            };
            inventory.ReserveSeats(30);

            // Act
            Action act = () => inventory.ReleaseSeats(40);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Cannot release more seats than are booked. Requested: 40, Booked: 30");
        }

        [TestMethod]
        public void ReleaseSeats_WithNegativeQuantity_ThrowsArgumentException()
        {
            // Arrange
            var inventory = new EventSectionInventory
            {
                Capacity = 100
            };
            inventory.ReserveSeats(50);

            // Act
            Action act = () => inventory.ReleaseSeats(-10);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("Quantity must be positive*");
        }

        [TestMethod]
        public void ValidateReservation_WithAvailableCapacity_ReturnsSuccess()
        {
            // Arrange
            var inventory = new EventSectionInventory
            {
                Capacity = 100,
                VenueSection = new VenueSection { Name = "Orchestra" }
            };

            // Act
            var result = inventory.ValidateReservation(25);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void ValidateReservation_WhenSoldOut_ReturnsFailure()
        {
            // Arrange
            var inventory = new EventSectionInventory
            {
                Capacity = 50,
                VenueSection = new VenueSection { Name = "Balcony" }
            };
            inventory.ReserveSeats(50);

            // Act
            var result = inventory.ValidateReservation(1);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Balcony");
            result.ErrorMessage.Should().Contain("sold out");
        }

        [TestMethod]
        public void ValidateReservation_InsufficientCapacity_ReturnsFailure()
        {
            // Arrange
            var inventory = new EventSectionInventory
            {
                Capacity = 100
            };
            inventory.ReserveSeats(90);

            // Act
            var result = inventory.ValidateReservation(20);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Insufficient capacity");
            result.ErrorMessage.Should().Contain("Requested: 20");
            result.ErrorMessage.Should().Contain("Available: 10");
        }

        [TestMethod]
        public void ValidateReservation_WithNegativeQuantity_ReturnsFailure()
        {
            // Arrange
            var inventory = new EventSectionInventory
            {
                Capacity = 100
            };

            // Act
            var result = inventory.ValidateReservation(-5);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Be("Quantity must be positive.");
        }

        [TestMethod]
        public void Price_CanBeSetForSectionInventory()
        {
            // Arrange
            var inventory = new EventSectionInventory
            {
                Capacity = 100,
                Price = 49.99m
            };

            // Act
            var price = inventory.Price;

            // Assert
            price.Should().Be(49.99m);
        }

        [TestMethod]
        public void AllocationMode_DefaultsToGeneralAdmission()
        {
            // Arrange & Act
            var inventory = new EventSectionInventory
            {
                Capacity = 100
            };

            // Assert
            inventory.AllocationMode.Should().Be(SeatAllocationMode.GeneralAdmission);
        }

        [TestMethod]
        public void AllocationMode_CanBeSetToReserved()
        {
            // Arrange
            var inventory = new EventSectionInventory
            {
                Capacity = 100,
                AllocationMode = SeatAllocationMode.Reserved
            };

            // Act
            var mode = inventory.AllocationMode;

            // Assert
            mode.Should().Be(SeatAllocationMode.Reserved);
        }
    }
}
