using EventBookingSystem.Domain.Entities;

namespace EventBookingSystem.Domain.Services
{
    /// <summary>
    /// Generic strategy interface for handling reservations for specific event types.
    /// </summary>
    /// <typeparam name="TEvent">The specific event type this strategy handles.</typeparam>
    public interface IReservationStrategy<TEvent> where TEvent : EventBase
    {
        /// <summary>
        /// Validates if a reservation can be made.
        /// </summary>
        /// <param name="venue">The venue hosting the event.</param>
        /// <param name="evnt">The event for which a reservation is being made.</param>
        /// <param name="request">The reservation request details.</param>
        /// <returns>A ValidationResult indicating success or failure.</returns>
        ValidationResult ValidateReservation(Venue venue, TEvent evnt, ReservationRequest request);
        
        /// <summary>
        /// Reserves tickets/seats for the event.
        /// </summary>
        /// <param name="venue">The venue hosting the event.</param>
        /// <param name="evnt">The event for which a reservation is being made.</param>
        /// <param name="request">The reservation request details.</param>
        void Reserve(Venue venue, TEvent evnt, ReservationRequest request);
    }
}
