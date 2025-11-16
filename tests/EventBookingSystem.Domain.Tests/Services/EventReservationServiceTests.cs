using AwesomeAssertions;
using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Domain.Services;
using EventBookingSystem.Domain.Tests.Helpers;

namespace EventBookingSystem.Domain.Tests.Services
{
    [TestClass]
    public class EventReservationServiceTests
    {
        [TestMethod]
        public void ReserveTickets_GeneralAdmission_ReservesSuccessfully()
        {
            // Arrange
            var service = new EventReservationService();
            var venue = TestDataBuilder.CreateVenueWithCapacity("Club", "123 St", 500);
            var evnt = new GeneralAdmissionEvent
            {
                Name = "DJ Night",
                Capacity = 500
            };

            // Act
            service.ReserveTickets(venue, evnt, 50);

            // Assert
            evnt.TotalReserved.Should().Be(50);
            evnt.AvailableCapacity.Should().Be(450);
        }

        [TestMethod]
        public void ReserveTickets_GeneralAdmission_ExceedingCapacity_ThrowsException()
        {
            // Arrange
            var service = new EventReservationService();
            var venue = TestDataBuilder.CreateVenueWithCapacity("Small Club", "456 St", 100);
            var evnt = new GeneralAdmissionEvent
            {
                Name = "Sold Out Show",
                Capacity = 100
            };
            evnt.ReserveTickets(100);

            // Act
            Action act = () => service.ReserveTickets(venue, evnt, 1);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*sold out*");
        }

        [TestMethod]
        public void ReserveInSection_SectionBased_ReservesInCorrectSection()
        {
            // Arrange
            var service = new EventReservationService();
            var venue = TestDataBuilder.CreateVenueWithSections(
                "Arena", "789 Ave",
                ("Floor", 500),
                ("Balcony", 300)
            );
            var evnt = new SectionBasedEvent
            {
                Name = "Concert",
                SectionInventories = new List<EventSectionInventory>
                {
                    new() { VenueSectionId = 1, Capacity = 500, Price = 100m },
                    new() { VenueSectionId = 2, Capacity = 300, Price = 60m }
                }
            };

            // Act
            service.ReserveInSection(venue, evnt, sectionId: 1, quantity: 25);

            // Assert
            var floor = evnt.GetSection(1);
            floor!.Booked.Should().Be(25);
            
            var balcony = evnt.GetSection(2);
            balcony!.Booked.Should().Be(0);
        }

        [TestMethod]
        public void ReserveInSection_SectionBased_InvalidSection_ThrowsException()
        {
            // Arrange
            var service = new EventReservationService();
            var venue = TestDataBuilder.CreateVenueWithCapacity("Venue", "Address", 100);
            var evnt = new SectionBasedEvent
            {
                Name = "Event",
                SectionInventories = new List<EventSectionInventory>
                {
                    new() { VenueSectionId = 1, Capacity = 100 }
                }
            };

            // Act
            Action act = () => service.ReserveInSection(venue, evnt, sectionId: 99, quantity: 10);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Section with ID 99 not found*");
        }

        [TestMethod]
        public void ReserveSeat_ReservedSeating_ReservesSpecificSeat()
        {
            // Arrange
            var service = new EventReservationService();
            var venue = TestDataBuilder.CreateVenueWithCapacity("Theatre", "Broadway", 200);
            var evnt = TestDataBuilder.CreateReservedSeatingEvent(venue, "Play", DateTime.Now.AddDays(30));

            // Act
            var firstSeat = evnt.Seats.First();
            service.ReserveSeat(venue, evnt, firstSeat.VenueSeatId);

            // Assert
            firstSeat.Status.Should().Be(SeatStatus.Reserved);
            evnt.TotalReserved.Should().Be(1);
        }

        [TestMethod]
        public void ReserveSeat_ReservedSeating_SeatNotAvailable_ThrowsException()
        {
            // Arrange
            var service = new EventReservationService();
            var venue = TestDataBuilder.CreateVenueWithCapacity("Theatre", "Broadway", 50);
            var evnt = TestDataBuilder.CreateReservedSeatingEvent(venue, "Play", DateTime.Now.AddDays(30));
            var seat = evnt.Seats.First();
            seat.Reserve();

            // Act
            Action act = () => service.ReserveSeat(venue, evnt, seat.VenueSeatId);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*not available*");
        }

        [TestMethod]
        public void ValidateGeneralAdmission_WithCapacity_ReturnsSuccess()
        {
            // Arrange
            var service = new EventReservationService();
            var venue = TestDataBuilder.CreateVenueWithCapacity("Venue", "Address", 500);
            var evnt = new GeneralAdmissionEvent
            {
                Name = "Event",
                Capacity = 500
            };
            evnt.ReserveTickets(200);

            // Act
            var result = service.ValidateGeneralAdmission(venue, evnt, 100);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void ValidateGeneralAdmission_InsufficientCapacity_ReturnsFailure()
        {
            // Arrange
            var service = new EventReservationService();
            var venue = TestDataBuilder.CreateVenueWithCapacity("Small Venue", "Address", 100);
            var evnt = new GeneralAdmissionEvent
            {
                Name = "Event",
                Capacity = 100
            };
            evnt.ReserveTickets(95);

            // Act
            var result = service.ValidateGeneralAdmission(venue, evnt, 10);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Insufficient capacity");
        }

        [TestMethod]
        public void ValidateSectionBased_ValidSection_ReturnsSuccess()
        {
            // Arrange
            var service = new EventReservationService();
            var venue = TestDataBuilder.CreateVenueWithCapacity("Venue", "Address", 500);
            var evnt = new SectionBasedEvent
            {
                Name = "Event",
                SectionInventories = new List<EventSectionInventory>
                {
                    new() { VenueSectionId = 1, Capacity = 500 }
                }
            };

            // Act
            var result = service.ValidateSectionBased(venue, evnt, sectionId: 1, quantity: 50);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void ValidateSectionBased_InvalidSection_ReturnsFailure()
        {
            // Arrange
            var service = new EventReservationService();
            var venue = TestDataBuilder.CreateVenueWithCapacity("Venue", "Address", 500);
            var evnt = new SectionBasedEvent
            {
                Name = "Event",
                SectionInventories = new List<EventSectionInventory>
                {
                    new() { VenueSectionId = 1, Capacity = 500 }
                }
            };

            // Act
            var result = service.ValidateSectionBased(venue, evnt, sectionId: 99, quantity: 10);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Section with ID 99 not found");
        }

        [TestMethod]
        public void ValidateReservedSeating_AvailableSeat_ReturnsSuccess()
        {
            // Arrange
            var service = new EventReservationService();
            var venue = TestDataBuilder.CreateVenueWithCapacity("Theatre", "Broadway", 100);
            var evnt = TestDataBuilder.CreateReservedSeatingEvent(venue, "Play", DateTime.Now.AddDays(30));
            var seat = evnt.Seats.First();

            // Act
            var result = service.ValidateReservedSeating(venue, evnt, seat.VenueSeatId);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void ValidateReservedSeating_SeatNotAvailable_ReturnsFailure()
        {
            // Arrange
            var service = new EventReservationService();
            var venue = TestDataBuilder.CreateVenueWithCapacity("Theatre", "Broadway", 100);
            var evnt = TestDataBuilder.CreateReservedSeatingEvent(venue, "Play", DateTime.Now.AddDays(30));
            var seat = evnt.Seats.First();
            seat.Reserve();

            // Act
            var result = service.ValidateReservedSeating(venue, evnt, seat.VenueSeatId);

            // Assert
            result.IsValid.Should().BeFalse();
            result.ErrorMessage.Should().Contain("not available");
        }
    }
}
