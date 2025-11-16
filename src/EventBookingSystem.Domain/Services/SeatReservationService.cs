using EventBookingSystem.Domain.Entities;

namespace EventBookingSystem.Domain.Services
{
    public class SeatReservationService
    {
        private readonly IEnumerable<ISeatingStrategy> _strategies;

        public SeatReservationService(IEnumerable<ISeatingStrategy> strategies)
        {
            _strategies = strategies ?? throw new ArgumentNullException(nameof(strategies));
        }

        public void ReserveSeat(Venue venue, Event evnt, int? sectionId = null)
        {
            var strategy = _strategies.FirstOrDefault(s => s.CanHandle(evnt.EventType));

            if (strategy == null)
            {
                throw new InvalidOperationException($"No seating strategy found for event type: {evnt.EventType}");
            }

            strategy.Reserve(venue, evnt, sectionId);
        }

        public ValidationResult ValidateReservation(Venue venue, Event evnt, int? sectionId = null)
        {
            var strategy = _strategies.FirstOrDefault(s => s.CanHandle(evnt.EventType));

            if (strategy == null)
            {
                return ValidationResult.Failure($"No seating strategy found for event type: {evnt.EventType}");
            }

            return strategy.ValidateReservation(venue, evnt, sectionId);
        }
    }
}
