using EventBookingSystem.Application.DTOs;

namespace EventBookingSystem.Application.Services
{
    /// <summary>
    /// Service for querying booking information.
    /// Follows SRP - single responsibility of reading booking data.
    /// Follows ISP - focused interface for booking queries only.
    /// </summary>
    public interface IBookingQueryService
    {
        /// <summary>
        /// Retrieves all bookings for a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A collection of booking DTOs for the user.</returns>
        Task<IEnumerable<BookingDto>> GetBookingsByUserIdAsync(int userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all bookings for events at a specific venue.
        /// </summary>
        /// <param name="venueId">The ID of the venue.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A collection of booking DTOs for the venue.</returns>
        Task<IEnumerable<BookingDto>> GetBookingsByVenueIdAsync(int venueId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a specific booking by its ID.
        /// </summary>
        /// <param name="bookingId">The ID of the booking.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The booking DTO if found, null otherwise.</returns>
        Task<BookingDto?> GetBookingByIdAsync(int bookingId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds bookings for users who have at least one successfully paid booking at the specified venue.
        /// Returns all bookings (paid and unpaid) for qualifying users at the specified venue.
        /// </summary>
        /// <param name="venueId">The ID of the venue.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A collection of booking DTOs for users with at least one paid booking at the venue.</returns>
        Task<IEnumerable<BookingDto>> GetBookingsForPaidUsersAtVenueAsync(int venueId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds all user IDs who have no bookings whatsoever at the specified venue.
        /// Useful for identifying potential customers for targeted marketing campaigns.
        /// </summary>
        /// <param name="venueId">The ID of the venue.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A collection of user IDs with no bookings at the venue.</returns>
        Task<IEnumerable<int>> GetUsersWithoutBookingsAtVenueAsync(int venueId, CancellationToken cancellationToken = default);
    }
}
