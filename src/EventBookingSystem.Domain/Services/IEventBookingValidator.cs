using EventBookingSystem.Domain.Entities;

namespace EventBookingSystem.Domain.Services
{
    public interface IEventBookingValidator
    {
        ValidationResult Validate(Venue venue, Event evnt);
    }
}
