using EventBookingSystem.Domain.Entities;

namespace EventBookingSystem.Application.Interfaces
{
    /// <summary>
    /// Repository interface for booking operations.
    /// Defined in Application layer to follow Dependency Inversion Principle.
    /// Implemented in Infrastructure layer.
    /// </summary>
    public interface IBookingRepository
    {
        Task<Booking?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Booking>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Booking>> GetByEventIdAsync(int eventId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Booking>> GetAllBookings();
        Task<Booking?> AddAsync(Booking booking, CancellationToken cancellationToken = default);
        Task<IEnumerable<Booking>> FindBookingsForPaidUsersAtVenueAsync(int venueId, CancellationToken cancellationToken = default);
        Task<IEnumerable<int>> FindUsersWithoutBookingsInVenueAsync(int venueId, CancellationToken cancellationToken = default);
    }
}
