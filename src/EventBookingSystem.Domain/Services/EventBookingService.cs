using EventBookingSystem.Domain.Entities;

namespace EventBookingSystem.Domain.Services
{
    /// <summary>
    /// Provides functionality to book events at a specified venue, ensuring that all configured event booking
    /// validators are applied before the booking is confirmed.
    /// </summary>
    /// <remarks>
    /// This service uses one or more implementations of <see cref="IEventBookingValidator"/> to
    /// validate event bookings. If any validator fails, the booking is not performed and an exception is thrown. This
    /// class is typically used to centralize event booking logic and enforce validation rules consistently across
    /// venues.
    /// </remarks>
    public class EventBookingService
    {
        private readonly IEventBookingValidator[] _validators;

        public EventBookingService(params IEventBookingValidator[] validators)
        {
            _validators = validators;
        }

        /// <summary>
        /// Attempts to book an event at the specified venue after validating it with all configured validators.
        /// </summary>
        /// <param name="venue">The venue where the event will be held.</param>
        /// <param name="evnt">The event to book (can be any EventBase type).</param>
        /// <exception cref="InvalidOperationException">Thrown if the event cannot be booked due to validation failures.</exception>
        public void BookEvent(Venue venue, EventBase evnt)
        {
            foreach (var validator in _validators)
            {
                var result = validator.Validate(venue, evnt);
                if (!result.IsValid)
                {
                    throw new InvalidOperationException(result.ErrorMessage);
                }
            }
            
            venue.BookEvent(evnt);
        }
        
        /// <summary>
        /// Validates if an event can be booked at the venue without actually booking it.
        /// </summary>
        /// <param name="venue">The venue where the event would be held.</param>
        /// <param name="evnt">The event to validate.</param>
        /// <returns>A ValidationResult indicating if the booking is valid.</returns>
        public ValidationResult ValidateBooking(Venue venue, EventBase evnt)
        {
            foreach (var validator in _validators)
            {
                var result = validator.Validate(venue, evnt);
                if (!result.IsValid)
                {
                    return result;
                }
            }
            
            return ValidationResult.Success();
        }
    }

    /// <summary>
    /// Validates that an event's estimated attendance does not exceed the venue's maximum capacity.
    /// </summary>
    public class CapacityValidator : IEventBookingValidator
    {
        public ValidationResult Validate(Venue venue, EventBase evnt)
        {
            return evnt.EstimatedAttendance > venue.MaxCapacity
                ? ValidationResult.Failure("The event exceeds the venue's maximum capacity.")
                : ValidationResult.Success();
        }
    }

    /// <summary>
    /// Validates that an event does not have time conflicts with existing events at the venue.
    /// </summary>
    public class TimeConflictValidator : IEventBookingValidator
    {
        public ValidationResult Validate(Venue venue, EventBase evnt)
        {
            var hasConflict = venue.Events.Any(e =>
                evnt.StartsAt < e.EndsAt && e.StartsAt < evnt.EndsAt);
            
            return hasConflict
                ? ValidationResult.Failure("The event conflicts with existing scheduled events at the venue.")
                : ValidationResult.Success();
        }
    }
}
