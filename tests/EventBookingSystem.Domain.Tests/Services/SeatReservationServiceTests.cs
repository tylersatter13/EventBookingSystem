using AwesomeAssertions;
using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Domain.Services;

namespace EventBookingSystem.Domain.Tests.Services
{
    [TestClass]
    public class SeatReservationServiceTests
    {
        [TestMethod]
        public void ReserveSeat_ShouldReserveSeatSuccessfully()
        {
            // Arrange
            var seatingStrategies = new List<ISeatingStrategy>() { new GeneralAdmissionStrategy() };
            var seatReservationService = new SeatReservationService(seatingStrategies);
            var venue = new Venue()
            {
                Name = "Test Venue",
                Address = "123 Test St",
                MaxCapacity = 100
            };
            var evnt = new Event()
            {
                Name = "Test Event",
                SeatsReservered = 0,
            };
            var expectedEventSeats = 1;

            // Act
            seatReservationService.ReserveSeat(venue, evnt);

            // Assert
            evnt.SeatsReservered.Should().Be(expectedEventSeats);
        }
        [TestMethod]
        public void ReserveSeat_ShouldThrowException_WhenNoStrategyFound()
        {
            // Arrange
            var seatingStrategies = new List<ISeatingStrategy>(); // No strategies provided
            var seatReservationService = new SeatReservationService(seatingStrategies);
            var venue = new Venue()
            {
                Name = "Test Venue",
                Address = "123 Test St",
                MaxCapacity = 100
            };
            var evnt = new Event()
            {
                Name = "Test Event",
                SeatsReservered = 0,
            };
            // Act
            Action act = () => seatReservationService.ReserveSeat(venue, evnt);
            // Assert
            act.Should().Throw<InvalidOperationException>()
                .Where(e => e.Message.StartsWith("No seating strategy found for event type: "));
        }
        [TestMethod]
        public void ReserveSeat_ShouldThrowException_WhenCapacityExceeded()
        {
            // Arrange
            var seatingStrategies = new List<ISeatingStrategy>() { new GeneralAdmissionStrategy() };
            var seatReservationService = new SeatReservationService(seatingStrategies);
            var venue = new Venue()
            {
                Name = "Test Venue",
                Address = "123 Test St",
                MaxCapacity = 1 // Set low capacity for testing
            };
            var evnt = new Event()
            {
                Name = "Test Event",
                SeatsReservered = 1, // Already at capacity
            };
            // Act
            Action act = () => seatReservationService.ReserveSeat(venue, evnt);
            // Assert
            act.Should().Throw<InvalidOperationException>().WithMessage("No available seats for this event.");
        }
    }
}
