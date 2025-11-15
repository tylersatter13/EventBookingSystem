using EventBookingSystem.Domain.Entities;

namespace EventBookingSystem.Domain.Services
{
    /// <summary>
    /// Provides functionality to book events at a specified venue, ensuring that all configured event booking
    /// validators are applied before the booking is confirmed.
    /// </summary>
    /// <remarks>This service uses one or more implementations of <see cref="IEventBookingValidator"/> to
    /// validate event bookings. If any validator fails, the booking is not performed and an exception is thrown. This
    /// class is typically used to centralize event booking logic and enforce validation rules consistently across
    /// venues.</remarks>
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
        /// <param name="venue"></param>
        /// <param name="evnt"></param>
        /// <exception cref="InvalidOperationException">Throws an exception if an event cannot be booked</exception>
        public void BookEvent(Venue venue, Event evnt)
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
    }

    public class CapacityValidator : IEventBookingValidator
    {
        public ValidationResult Validate(Venue venue, Event evnt)
        {
            return evnt.EstimatedAttendance > venue.MaxCapacity
                ? ValidationResult.Failure("The event exceeds the venue's maximum capacity.")
                : ValidationResult.Success();
        }
    }

    public class TimeConflictValidator : IEventBookingValidator
    {
        public ValidationResult Validate(Venue venue, Event evnt)
        {
            var hasConflict = venue.Events.Any(e =>
                evnt.StartsAt < e.EndsAt && e.StartsAt < evnt.EndsAt);
            
            return hasConflict
                ? ValidationResult.Failure("The event conflicts with existing scheduled events at the venue.")
                : ValidationResult.Success();
        }
    }
}
