using AwesomeAssertions;
using EventBookingSystem.Domain.Entities;

namespace EventBookingSystem.Domain.Tests.Entities
{
    [TestClass]
    public class EventSeatTests
    {
        [TestMethod]
        public void IsAvailable_WhenStatusIsAvailable_ReturnsTrue()
        {
            // Arrange
            var eventSeat = new EventSeat
            {
                Id = 1,
                EventId = 1,
                VenueSeatId = 1,
                Status = SeatStatus.Available
            };

            // Act
            var result = eventSeat.IsAvailable();

            // Assert
            result.Should().BeTrue(because: "the seat status is Available");
        }

        [TestMethod]
        public void IsAvailable_WhenStatusIsReserved_ReturnsFalse()
        {
            // Arrange
            var eventSeat = new EventSeat
            {
                Id = 1,
                EventId = 1,
                VenueSeatId = 1,
                Status = SeatStatus.Reserved
            };

            // Act
            var result = eventSeat.IsAvailable();

            // Assert
            result.Should().BeFalse(because: "the seat status is Reserved");
        }

        [TestMethod]
        public void IsAvailable_WhenStatusIsLocked_ReturnsFalse()
        {
            // Arrange
            var eventSeat = new EventSeat
            {
                Id = 1,
                EventId = 1,
                VenueSeatId = 1,
                Status = SeatStatus.Locked
            };

            // Act
            var result = eventSeat.IsAvailable();

            // Assert
            result.Should().BeFalse(because: "the seat status is Locked");
        }

        [TestMethod]
        public void IsAvailable_WhenCreatedWithDefaultStatus_ReturnsTrue()
        {
            // Arrange
            var eventSeat = new EventSeat
            {
                Id = 1,
                EventId = 1,
                VenueSeatId = 1
            };

            // Act
            var result = eventSeat.IsAvailable();

            // Assert
            result.Should().BeTrue(because: "the default status is Available");
        }


        [TestMethod]
        public void Reserve_WhenSeatIsAvailable_ChangesStatusToReserved()
        {
            // Arrange
            var eventSeat = new EventSeat
            {
                Id = 1,
                EventId = 1,
                VenueSeatId = 1,
                Status = SeatStatus.Available
            };

            // Act
            eventSeat.Reserve();

            // Assert
            eventSeat.Status.Should().Be(SeatStatus.Reserved, because: "the seat was successfully reserved");
        }

        [TestMethod]
        public void Reserve_WhenSeatIsReserved_ThrowsInvalidOperationException()
        {
            // Arrange
            var eventSeat = new EventSeat
            {
                Id = 1,
                EventId = 1,
                VenueSeatId = 1,
                Status = SeatStatus.Reserved
            };

            // Act
            Action act = () => eventSeat.Reserve();

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*is not available*")
                .WithMessage("*Reserved*", because: "the seat is already reserved");
        }

        [TestMethod]
        public void Reserve_WhenSeatIsLocked_ThrowsInvalidOperationException()
        {
            // Arrange
            var eventSeat = new EventSeat
            {
                Id = 1,
                EventId = 1,
                VenueSeatId = 1,
                Status = SeatStatus.Locked
            };

            // Act
            Action act = () => eventSeat.Reserve();

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*is not available*")
                .WithMessage("*Locked*", because: "the seat is locked");
        }

        [TestMethod]
        public void Reserve_WhenSeatIsReserved_DoesNotChangeStatus()
        {
            // Arrange
            var eventSeat = new EventSeat
            {
                Id = 1,
                EventId = 1,
                VenueSeatId = 1,
                Status = SeatStatus.Reserved
            };

            // Act
            try
            {
                eventSeat.Reserve();
            } 
            catch(InvalidOperationException)
            {
                // Expected exception
            }

            // Assert
            eventSeat.Status.Should().Be(SeatStatus.Reserved, because: "the status should not change after a failed reserve attempt");
        }


        [TestMethod]
        public void Lock_WhenSeatIsAvailable_ChangesStatusToLocked()
        {
            // Arrange
            var eventSeat = new EventSeat
            {
                Id = 1,
                EventId = 1,
                VenueSeatId = 1,
                Status = SeatStatus.Available
            };

            // Act
            eventSeat.Lock();

            // Assert
            eventSeat.Status.Should().Be(SeatStatus.Locked, because: "the seat was successfully locked");
        }

        [TestMethod]
        public void Lock_WhenSeatIsReserved_ThrowsInvalidOperationException()
        {
            // Arrange
            var eventSeat = new EventSeat
            {
                Id = 1,
                EventId = 1,
                VenueSeatId = 1,
                Status = SeatStatus.Reserved
            };

            // Act
            Action act = () => eventSeat.Lock();

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Cannot lock seat*")
                .WithMessage("*Reserved*", because: "the seat is already reserved");
        }

        [TestMethod]
        public void Lock_WhenSeatIsLocked_ThrowsInvalidOperationException()
        {
            // Arrange
            var eventSeat = new EventSeat
            {
                Id = 1,
                EventId = 1,
                VenueSeatId = 1,
                Status = SeatStatus.Locked
            };

            // Act
            Action act = () => eventSeat.Lock();

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Cannot lock seat*")
                .WithMessage("*Locked*", because: "the seat is already locked");
        }

        [TestMethod]
        public void Lock_WhenSeatIsReserved_DoesNotChangeStatus()
        {
            // Arrange
            var eventSeat = new EventSeat
            {
                Id = 1,
                EventId = 1,
                VenueSeatId = 1,
                Status = SeatStatus.Reserved
            };

            // Act
            try
            {
                eventSeat.Lock();
            }
            catch (InvalidOperationException)
            {
                // Expected exception
            }

            // Assert
            eventSeat.Status.Should().Be(SeatStatus.Reserved, because: "the status should not change after a failed lock attempt");
        }

        [TestMethod]
        public void Release_WhenSeatIsLocked_ChangesStatusToAvailable()
        {
            // Arrange
            var eventSeat = new EventSeat
            {
                Id = 1,
                EventId = 1,
                VenueSeatId = 1,
                Status = SeatStatus.Locked
            };

            // Act
            eventSeat.Release();

            // Assert
            eventSeat.Status.Should().Be(SeatStatus.Available, because: "the locked seat was successfully released");
        }

        [TestMethod]
        public void Release_WhenSeatIsAvailable_ThrowsInvalidOperationException()
        {
            // Arrange
            var eventSeat = new EventSeat
            {
                Id = 1,
                EventId = 1,
                VenueSeatId = 1,
                Status = SeatStatus.Available
            };

            // Act
            Action act = () => eventSeat.Release();

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Cannot release seat*")
                .WithMessage("*Only locked seats can be released*")
                .WithMessage("*Available*", because: "the seat is not locked");
        }

        [TestMethod]
        public void Release_WhenSeatIsReserved_ThrowsInvalidOperationException()
        {
            // Arrange
            var eventSeat = new EventSeat
            {
                Id = 1,
                EventId = 1,
                VenueSeatId = 1,
                Status = SeatStatus.Reserved
            };

            // Act
            Action act = () => eventSeat.Release();

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Cannot release seat*")
                .WithMessage("*Only locked seats can be released*")
                .WithMessage("*Reserved*", because: "the seat is not locked");
        }

        [TestMethod]
        public void Release_WhenSeatIsAvailable_DoesNotChangeStatus()
        {
            // Arrange
            var eventSeat = new EventSeat
            {
                Id = 1,
                EventId = 1,
                VenueSeatId = 1,
                Status = SeatStatus.Available
            };

            // Act
            try
            {
                eventSeat.Release();
            }
            catch (InvalidOperationException)
            {
                // Expected exception
            }

            // Assert
            eventSeat.Status.Should().Be(SeatStatus.Available, because: "the status should not change after a failed release attempt");
        }

        [TestMethod]
        public void StateTransition_AvailableToLockedToAvailable_WorksCorrectly()
        {
            // Arrange
            var eventSeat = new EventSeat
            {
                Id = 1,
                EventId = 1,
                VenueSeatId = 1,
                Status = SeatStatus.Available
            };

            // Act & Assert - Lock the seat
            eventSeat.Lock();
            eventSeat.Status.Should().Be(SeatStatus.Locked);

            // Act & Assert - Release the seat
            eventSeat.Release();
            eventSeat.Status.Should().Be(SeatStatus.Available);
        }

        [TestMethod]
        public void StateTransition_AvailableToReserved_WorksCorrectly()
        {
            // Arrange
            var eventSeat = new EventSeat
            {
                Id = 1,
                EventId = 1,
                VenueSeatId = 1,
                Status = SeatStatus.Available
            };

            // Act
            eventSeat.Reserve();

            // Assert
            eventSeat.Status.Should().Be(SeatStatus.Reserved);
            eventSeat.IsAvailable().Should().BeFalse();
        }

        [TestMethod]
        public void StateTransition_LockedToReserved_ThrowsException()
        {
            // Arrange
            var eventSeat = new EventSeat
            {
                Id = 1,
                EventId = 1,
                VenueSeatId = 1,
                Status = SeatStatus.Locked
            };

            // Act
            Action act = () => eventSeat.Reserve();

            // Assert
            act.Should().Throw<InvalidOperationException>(because: "cannot reserve a locked seat directly");
            eventSeat.Status.Should().Be(SeatStatus.Locked, because: "the status should remain unchanged");
        }
    }
}
