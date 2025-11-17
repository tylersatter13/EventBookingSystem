using EventBookingSystem.Domain.Entities;

namespace EventBookingSystem.Infrastructure.Interfaces
{
    /// <summary>
    /// Repository interface for Booking entity operations.
    /// </summary>
    public interface IBookingRepository : IRepository<Booking>
    {
        /// <summary>
        /// Gets all bookings for a specific user.
        /// </summary>
        /// <param name="userId">The user ID to search for.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A collection of bookings for the user.</returns>
        Task<IEnumerable<Booking>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all bookings for a specific event.
        /// </summary>
        /// <param name="eventId">The event ID to search for.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A collection of bookings for the event.</returns>
        Task<IEnumerable<Booking>> GetByEventIdAsync(int eventId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all bookings.
        /// </summary>
        /// <returns>A collection of all bookings.</returns>
        Task<IEnumerable<Booking>> GetAllBookings();

        /// <summary>
        /// Finds bookings for users who have at least one successfully paid booking at the specified venue.
        /// Returns all bookings (paid and unpaid) for qualifying users at the specified venue.
        /// </summary>
        /// <param name="venueId">The venue ID to search for.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A collection of bookings for users with at least one paid booking at the venue.</returns>
        Task<IEnumerable<Booking>> FindBookingsForPaidUsersAtVenueAsync(int venueId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds all user IDs who have no bookings whatsoever at the specified venue.
        /// Returns user IDs from the Users table who do not appear in any bookings for events at the venue.
        /// </summary>
        /// <param name="venueId">The venue ID to search for.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A collection of user IDs with no bookings at the venue.</returns>
        Task<IEnumerable<int>> FindUsersWithoutBookingsInVenueAsync(int venueId, CancellationToken cancellationToken = default);
    }
}
