using EventBookingSystem.Domain.Entities;

namespace EventBookingSystem.Domain.Services
{
    public class EventBookingService
    {
        private readonly IEventBookingValidator[] _validators;

        public EventBookingService(params IEventBookingValidator[] validators)
        {
            _validators = validators;
        }

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
