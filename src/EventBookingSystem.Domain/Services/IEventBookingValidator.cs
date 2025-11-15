using EventBookingSystem.Domain.Entities;

namespace EventBookingSystem.Domain.Services
{
    public interface IEventBookingValidator
    {
        ValidationResult Validate(Venue venue, Event evnt);
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        
        public static ValidationResult Success() => new() { IsValid = true };
        public static ValidationResult Failure(string message) => new() { IsValid = false, ErrorMessage = message };
    }
}
