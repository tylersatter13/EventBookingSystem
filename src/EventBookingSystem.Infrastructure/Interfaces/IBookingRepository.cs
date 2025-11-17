using EventBookingSystem.Domain.Entities;

namespace EventBookingSystem.Infrastructure.Interfaces
{
    /// <summary>
    /// Repository interface for Booking entity operations.
    /// </summary>
    public interface IBookingRepository : IRespository<Booking>
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

        Task<IEnumerable<Booking>> GetAllBookings();
    }
}
