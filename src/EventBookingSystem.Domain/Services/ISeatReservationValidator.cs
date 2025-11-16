using EventBookingSystem.Domain.Entities;

namespace EventBookingSystem.Domain.Services
{
    public interface ISeatReservationValidator
    {
        ValidationResult Validate(Venue venue, Event evnt);
    }
}
