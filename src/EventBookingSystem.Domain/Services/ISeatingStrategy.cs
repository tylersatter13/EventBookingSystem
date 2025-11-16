using EventBookingSystem.Domain.Entities;

namespace EventBookingSystem.Domain.Services
{
    /// <summary>
    /// Defines the contract for different event seating strategies.
    /// </summary>
    public interface ISeatingStrategy
    {
        /// <summary>
        /// Determines if this strategy can handle the given event type.
        /// </summary>
        bool CanHandle(EventType eventType);

        /// <summary>
        /// Reserves a seat or admission for the event using the specific strategy.
        /// </summary>
        /// <param name="venue">The venue hosting the event.</param>
        /// <param name="evnt">The event for which seating is being reserved.</param>
        /// <param name="sectionId">Optional section identifier for section-based reservations.</param>
        void Reserve(Venue venue, Event evnt, int? sectionId = null);

        /// <summary>
        /// Validates if a reservation can be made according to this strategy.
        /// </summary>
        ValidationResult ValidateReservation(Venue venue, Event evnt, int? sectionId = null);
    }
}
