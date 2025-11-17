using EventBookingSystem.Domain.Entities;

namespace EventBookingSystem.Domain.Services
{
    /// <summary>
    /// Defines the contract for booking-related operations.
    /// Follows DIP - allows Application Layer to depend on abstraction, not concrete implementation.
    /// </summary>
    public interface IBookingService
    {
        /// <summary>
        /// Creates a new booking after validating all business rules.
        /// </summary>
        /// <param name="user">The user making the booking.</param>
        /// <param name="evnt">The event being booked.</param>
        /// <param name="reservationRequest">The reservation details.</param>
        /// <returns>The created booking.</returns>
        /// <exception cref="InvalidOperationException">Thrown when validation fails.</exception>
        Booking CreateBooking(User user, EventBase evnt, ReservationRequest reservationRequest);

        /// <summary>
        /// Validates if a booking can be created.
        /// Orchestrates multiple validators following the Composite pattern.
        /// </summary>
        /// <param name="user">The user making the booking.</param>
        /// <param name="evnt">The event being booked.</param>
        /// <param name="reservationRequest">The reservation details.</param>
        /// <returns>A ValidationResult indicating success or failure.</returns>
        ValidationResult ValidateBooking(User user, EventBase evnt, ReservationRequest reservationRequest);
    }
}
