using AwesomeAssertions;
using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Domain.Tests.Helpers;

namespace EventBookingSystem.Domain.Tests.Entities
{
    [TestClass]
    public class ReservedSeatingEventTests
    {
        [TestMethod]
        public void TotalCapacity_ReturnsCountOfSeats()
        {
            // Arrange
            var evnt = new ReservedSeatingEvent
            {
                Name = "Theatre Show",
                Seats = new List<EventSeat>
                {
                    new() { Id = 1, VenueSeatId = 1, Status = SeatStatus.Available },
                    new() { Id = 2, VenueSeatId = 2, Status = SeatStatus.Available },
                    new() { Id = 3, VenueSeatId = 3, Status = SeatStatus.Reserved }
                }
            };

            // Act
            var capacity = evnt.TotalCapacity;

            // Assert
            capacity.Should().Be(3);
        }

        [TestMethod]
        public void TotalReserved_CountsOnlyReservedSeats()
        {
            // Arrange
            var evnt = new ReservedSeatingEvent
            {
                Name = "Show",
                Seats = new List<EventSeat>
                {
                    new() { Id = 1, VenueSeatId = 1, Status = SeatStatus.Available },
                    new() { Id = 2, VenueSeatId = 2, Status = SeatStatus.Reserved },
                    new() { Id = 3, VenueSeatId = 3, Status = SeatStatus.Reserved },
                    new() { Id = 4, VenueSeatId = 4, Status = SeatStatus.Locked }
                }
            };

            // Act
            var reserved = evnt.TotalReserved;

            // Assert
            reserved.Should().Be(2, because: "only Reserved status should be counted");
        }

        [TestMethod]
        public void IsSoldOut_WithAvailableSeats_ReturnsFalse()
        {
            // Arrange
            var evnt = new ReservedSeatingEvent
            {
                Name = "Show",
                Seats = new List<EventSeat>
                {
                    new() { Id = 1, VenueSeatId = 1, Status = SeatStatus.Available },
                    new() { Id = 2, VenueSeatId = 2, Status = SeatStatus.Reserved }
                }
            };

            // Act
            var isSoldOut = evnt.IsSoldOut;

            // Assert
            isSoldOut.Should().BeFalse();
        }

        [TestMethod]
        public void IsSoldOut_AllSeatsReservedOrLocked_ReturnsTrue()
        {
            // Arrange
            var evnt = new ReservedSeatingEvent
            {
                Name = "Sold Out Show",
                Seats = new List<EventSeat>
                {
                    new() { Id = 1, VenueSeatId = 1, Status = SeatStatus.Reserved },
                    new() { Id = 2, VenueSeatId = 2, Status = SeatStatus.Reserved },
                    new() { Id = 3, VenueSeatId = 3, Status = SeatStatus.Locked }
                }
            };

            // Act
            var isSoldOut = evnt.IsSoldOut;

            // Assert
            isSoldOut.Should().BeTrue();
        }

        [TestMethod]
        public void GetAvailableSeats_ReturnsOnlyAvailableSeats()
        {
            // Arrange
            var evnt = new ReservedSeatingEvent
            {
                Name = "Show",
                Seats = new List<EventSeat>
                {
                    new() { Id = 1, VenueSeatId = 1, Status = SeatStatus.Available },
                    new() { Id = 2, VenueSeatId = 2, Status = SeatStatus.Reserved },
                    new() { Id = 3, VenueSeatId = 3, Status = SeatStatus.Available },
                    new() { Id = 4, VenueSeatId = 4, Status = SeatStatus.Locked }
                }
            };

            // Act
            var availableSeats = evnt.GetAvailableSeats();

            // Assert
            availableSeats.Should().HaveCount(2);
            availableSeats.Should().Contain(s => s.VenueSeatId == 1);
            availableSeats.Should().Contain(s => s.VenueSeatId == 3);
        }

        [TestMethod]
        public void GetReservedSeats_ReturnsOnlyReservedSeats()
        {
            // Arrange
            var evnt = new ReservedSeatingEvent
            {
                Name = "Show",
                Seats = new List<EventSeat>
                {
                    new() { Id = 1, VenueSeatId = 1, Status = SeatStatus.Available },
                    new() { Id = 2, VenueSeatId = 2, Status = SeatStatus.Reserved },
                    new() { Id = 3, VenueSeatId = 3, Status = SeatStatus.Reserved }
                }
            };

            // Act
            var reservedSeats = evnt.GetReservedSeats();

            // Assert
            reservedSeats.Should().HaveCount(2);
            reservedSeats.Should().Contain(s => s.VenueSeatId == 2);
            reservedSeats.Should().Contain(s => s.VenueSeatId == 3);
        }

        [TestMethod]
        public void GetLockedSeats_ReturnsOnlyLockedSeats()
        {
            // Arrange
            var evnt = new ReservedSeatingEvent
            {
                Name = "Show",
                Seats = new List<EventSeat>
                {
                    new() { Id = 1, VenueSeatId = 1, Status = SeatStatus.Available },
                    new() { Id = 2, VenueSeatId = 2, Status = SeatStatus.Locked },
                    new() { Id = 3, VenueSeatId = 3, Status = SeatStatus.Reserved }
                }
            };

            // Act
            var lockedSeats = evnt.GetLockedSeats();

            // Assert
            lockedSeats.Should().HaveCount(1);
            lockedSeats.First().VenueSeatId.Should().Be(2);
        }

        [TestMethod]
        public void GetSeat_WithValidId_ReturnsSeat()
        {
            // Arrange
            var evnt = new ReservedSeatingEvent
            {
                Name = "Show",
                Seats = new List<EventSeat>
                {
                    new() { Id = 1, VenueSeatId = 42, Status = SeatStatus.Available },
                    new() { Id = 2, VenueSeatId = 43, Status = SeatStatus.Available }
                }
            };

            // Act
            var seat = evnt.GetSeat(42);

            // Assert
            seat.Should().NotBeNull();
            seat!.VenueSeatId.Should().Be(42);
        }

        [TestMethod]
        public void GetSeat_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var evnt = new ReservedSeatingEvent
            {
                Name = "Show",
                Seats = new List<EventSeat>
                {
                    new() { Id = 1, VenueSeatId = 42, Status = SeatStatus.Available }
                }
            };

            // Act
            var seat = evnt.GetSeat(99);

            // Assert
            seat.Should().BeNull();
        }

        [TestMethod]
        public void ReserveSeat_AvailableSeat_ReservesSeat()
        {
            // Arrange
            var evnt = new ReservedSeatingEvent
            {
                Name = "Show",
                Seats = new List<EventSeat>
                {
                    new() { Id = 1, VenueSeatId = 42, Status = SeatStatus.Available }
                }
            };

            // Act
            evnt.ReserveSeat(42);

            // Assert
            var seat = evnt.GetSeat(42);
            seat!.Status.Should().Be(SeatStatus.Reserved);
            evnt.TotalReserved.Should().Be(1);
        }

        [TestMethod]
        public void ReserveSeat_SeatNotFound_ThrowsException()
        {
            // Arrange
            var evnt = new ReservedSeatingEvent
            {
                Name = "Show",
                Seats = new List<EventSeat>
                {
                    new() { Id = 1, VenueSeatId = 42, Status = SeatStatus.Available }
                }
            };

            // Act
            Action act = () => evnt.ReserveSeat(99);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Seat with ID 99 not found*");
        }

        [TestMethod]
        public void ReserveSeat_SeatAlreadyReserved_ThrowsException()
        {
            // Arrange
            var evnt = new ReservedSeatingEvent
            {
                Name = "Show",
                Seats = new List<EventSeat>
                {
                    new() { Id = 1, VenueSeatId = 42, Status = SeatStatus.Reserved }
                }
            };

            // Act
            Action act = () => evnt.ReserveSeat(42);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*not available*");
        }

        [TestMethod]
        public void LockSeat_AvailableSeat_LocksSeat()
        {
            // Arrange
            var evnt = new ReservedSeatingEvent
            {
                Name = "Show",
                Seats = new List<EventSeat>
                {
                    new() { Id = 1, VenueSeatId = 42, Status = SeatStatus.Available }
                }
            };

            // Act
            evnt.LockSeat(42);

            // Assert
            var seat = evnt.GetSeat(42);
            seat!.Status.Should().Be(SeatStatus.Locked);
        }

        [TestMethod]
        public void ReleaseSeat_LockedSeat_ReleasesToAvailable()
        {
            // Arrange
            var evnt = new ReservedSeatingEvent
            {
                Name = "Show",
                Seats = new List<EventSeat>
                {
                    new() { Id = 1, VenueSeatId = 42, Status = SeatStatus.Locked }
                }
            };

            // Act
            evnt.ReleaseSeat(42);

            // Assert
            var seat = evnt.GetSeat(42);
            seat!.Status.Should().Be(SeatStatus.Available);
        }

        [TestMethod]
        public void GetSeatsInSection_ReturnsSeatsInSpecifiedSection()
        {
            // Arrange
            var evnt = new ReservedSeatingEvent
            {
                Name = "Show",
                Seats = new List<EventSeat>
                {
                    new() 
                    { 
                        Id = 1, 
                        VenueSeatId = 1, 
                        Status = SeatStatus.Available,
                        VenueSeat = new VenueSeat 
                        { 
                            Id = 1, 
                            VenueSectionId = 10, 
                            Row = "A", 
                            SeatNumber = "1" 
                        }
                    },
                    new() 
                    { 
                        Id = 2, 
                        VenueSeatId = 2, 
                        Status = SeatStatus.Available,
                        VenueSeat = new VenueSeat 
                        { 
                            Id = 2, 
                            VenueSectionId = 10, 
                            Row = "A", 
                            SeatNumber = "2" 
                        }
                    },
                    new() 
                    { 
                        Id = 3, 
                        VenueSeatId = 3, 
                        Status = SeatStatus.Available,
                        VenueSeat = new VenueSeat 
                        { 
                            Id = 3, 
                            VenueSectionId = 20, 
                            Row = "B", 
                            SeatNumber = "1" 
                        }
                    }
                }
            };

            // Act
            var seatsInSection10 = evnt.GetSeatsInSection(10);

            // Assert
            seatsInSection10.Should().HaveCount(2);
            seatsInSection10.Should().Contain(s => s.VenueSeatId == 1);
            seatsInSection10.Should().Contain(s => s.VenueSeatId == 2);
        }

        [TestMethod]
        public void GetAvailableSeatsInSection_ReturnsOnlyAvailableSeatsInSection()
        {
            // Arrange
            var evnt = new ReservedSeatingEvent
            {
                Name = "Show",
                Seats = new List<EventSeat>
                {
                    new() 
                    { 
                        VenueSeatId = 1, 
                        Status = SeatStatus.Available,
                        VenueSeat = new VenueSeat { Id = 1, VenueSectionId = 10, Row = "A", SeatNumber = "1" }
                    },
                    new() 
                    { 
                        VenueSeatId = 2, 
                        Status = SeatStatus.Reserved,
                        VenueSeat = new VenueSeat { Id = 2, VenueSectionId = 10, Row = "A", SeatNumber = "2" }
                    },
                    new() 
                    { 
                        VenueSeatId = 3, 
                        Status = SeatStatus.Available,
                        VenueSeat = new VenueSeat { Id = 3, VenueSectionId = 10, Row = "A", SeatNumber = "3" }
                    }
                }
            };

            // Act
            var availableInSection = evnt.GetAvailableSeatsInSection(10);

            // Assert
            availableInSection.Should().HaveCount(2);
            availableInSection.Should().Contain(s => s.VenueSeatId == 1);
            availableInSection.Should().Contain(s => s.VenueSeatId == 3);
        }
    }
}
