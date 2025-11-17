using EventBookingSystem.Domain.Entities;
using System;
using System.Linq;

namespace EventBookingSystem.Domain.Services
{
    /// <summary>
    /// Validates that a user hasn't exceeded their booking limit for an event.
    /// Example: Maximum 4 tickets per person.
    /// </summary>
    public class UserBookingLimitValidator : IBookingValidator
    {
        private const int MaxTicketsPerUser = 4;

        public ValidationResult Validate(User user, EventBase evnt, ReservationRequest request)
        {
            // Check how many bookings the user already has for this event
            var existingBookings = user.Bookings?
                .Where(b => b.Event.Id == evnt.Id && b.PaymentStatus != PaymentStatus.Refunded)
                .Sum(b => b.BookingItems.Sum(item => item.Quantity)) ?? 0;

            var totalAfterThisBooking = existingBookings + request.Quantity;

            if (totalAfterThisBooking > MaxTicketsPerUser)
            {
                return ValidationResult.Failure(
                    $"Booking limit exceeded. Maximum {MaxTicketsPerUser} tickets per person. " +
                    $"You already have {existingBookings} ticket(s).");
            }

            return ValidationResult.Success();
        }
    }

    /// <summary>
    /// Validates that the event is still accepting bookings (not in the past, not cancelled).
    /// </summary>
    public class EventAvailabilityValidator : IBookingValidator
    {
        public ValidationResult Validate(User user, EventBase evnt, ReservationRequest request)
        {
            // Check if event has already started or passed
            if (evnt.StartsAt <= DateTime.UtcNow)
            {
                return ValidationResult.Failure("Cannot book tickets for an event that has already started.");
            }

            // Check if event is sold out
            if (evnt.IsSoldOut)
            {
                return ValidationResult.Failure($"Event '{evnt.Name}' is sold out.");
            }

            return ValidationResult.Success();
        }
    }

    /// <summary>
    /// Validates that the user has provided required information.
    /// </summary>
    public class UserInformationValidator : IBookingValidator
    {
        public ValidationResult Validate(User user, EventBase evnt, ReservationRequest request)
        {
            if (user == null)
            {
                return ValidationResult.Failure("User information is required.");
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                return ValidationResult.Failure("User email is required for booking confirmation.");
            }

            if (string.IsNullOrWhiteSpace(user.Name))
            {
                return ValidationResult.Failure("User name is required.");
            }

            return ValidationResult.Success();
        }
    }

    /// <summary>
    /// Validates booking quantity is within acceptable limits.
    /// </summary>
    public class BookingQuantityValidator : IBookingValidator
    {
        private const int MinQuantity = 1;
        private const int MaxQuantity = 10;

        public ValidationResult Validate(User user, EventBase evnt, ReservationRequest request)
        {
            if (request.Quantity < MinQuantity)
            {
                return ValidationResult.Failure($"Minimum booking quantity is {MinQuantity}.");
            }

            if (request.Quantity > MaxQuantity)
            {
                return ValidationResult.Failure($"Maximum booking quantity is {MaxQuantity} tickets per transaction.");
            }

            return ValidationResult.Success();
        }
    }
}
