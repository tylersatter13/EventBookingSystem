using EventBookingSystem.Application.Models;
using EventBookingSystem.Application.Services;
using EventBookingSystem.Domain;
using EventBookingSystem.Domain.Entities;

namespace EventBookingSystem.Application.Validators
{
    /// <summary>
    /// Validates that the command has required fields.
    /// Follows SRP - single responsibility of basic input validation.
    /// </summary>
    public class RequiredFieldsValidator : IBookingCommandValidator
    {
        public Task<ValidationResult> ValidateAsync(CreateBookingCommand command)
        {
            if (command == null)
            {
                return Task.FromResult(ValidationResult.Failure("Command cannot be null"));
            }

            if (command.UserId <= 0)
            {
                return Task.FromResult(ValidationResult.Failure("Valid User ID is required"));
            }

            if (command.EventId <= 0)
            {
                return Task.FromResult(ValidationResult.Failure("Valid Event ID is required"));
            }

            if (command.Quantity <= 0)
            {
                return Task.FromResult(ValidationResult.Failure("Quantity must be greater than zero"));
            }

            return Task.FromResult(ValidationResult.Success());
        }
    }

    /// <summary>
    /// Validates business rules around booking quantities.
    /// Follows SRP - single responsibility of quantity validation.
    /// </summary>
    public class QuantityLimitsValidator : IBookingCommandValidator
    {
        private const int MaxQuantityPerBooking = 10;

        public Task<ValidationResult> ValidateAsync(CreateBookingCommand command)
        {
            if (command.Quantity > MaxQuantityPerBooking)
            {
                return Task.FromResult(ValidationResult.Failure(
                    $"Cannot book more than {MaxQuantityPerBooking} tickets per booking"));
            }

            return Task.FromResult(ValidationResult.Success());
        }
    }

    /// <summary>
    /// Validates that section-based bookings have a section ID.
    /// Follows SRP - single responsibility of request completeness validation.
    /// </summary>
    public class SectionRequirementValidator : IBookingCommandValidator
    {
        public Task<ValidationResult> ValidateAsync(CreateBookingCommand command)
        {
            // This is a simplified check - in reality, you'd need to know the event type
            // You might load event type here or inject a repository
            // For now, we'll allow it to pass and let domain validation handle it
            return Task.FromResult(ValidationResult.Success());
        }
    }
}
