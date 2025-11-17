using EventBookingSystem.Domain.Entities;

namespace EventBookingSystem.Domain.Services
{
    /// <summary>
    /// Defines a contract for validating booking-specific business rules.
    /// This allows for extensible validation following the Open/Closed Principle.
    /// </summary>
    public interface IBookingValidator
    {
        /// <summary>
        /// Validates if a booking can be created based on custom business rules.
        /// </summary>
        /// <param name="user">The user making the booking.</param>
        /// <param name="evnt">The event being booked.</param>
        /// <param name="request">The reservation request details.</param>
        /// <returns>A ValidationResult indicating success or failure.</returns>
        ValidationResult Validate(User user, EventBase evnt, ReservationRequest request);
    }
}
