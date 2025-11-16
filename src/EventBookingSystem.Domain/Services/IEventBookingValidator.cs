using EventBookingSystem.Domain.Entities;

namespace EventBookingSystem.Domain.Services
{
    /// <summary>
    /// Defines a contract for validating event bookings at a venue.
    /// </summary>
    public interface IEventBookingValidator
    {
        /// <summary>
        /// Validates if an event can be booked at the specified venue.
        /// </summary>
        /// <param name="venue">The venue where the event would be held.</param>
        /// <param name="evnt">The event to validate (can be any EventBase type).</param>
        /// <returns>A ValidationResult indicating success or failure with an error message.</returns>
        ValidationResult Validate(Venue venue, EventBase evnt);
    }
}
