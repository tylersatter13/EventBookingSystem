using EventBookingSystem.Domain.Entities;

namespace EventBookingSystem.Application.Interfaces
{
    /// <summary>
    /// Repository interface for event operations.
    /// Defined in Application layer to follow Dependency Inversion Principle.
    /// Implemented in Infrastructure layer.
    /// </summary>
    public interface IEventRepository
    {
        Task<EventBase?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<EventBase?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);
        Task<IEnumerable<EventBase>> GetByVenueIdAsync(int venueId, CancellationToken cancellationToken = default);
        Task<IEnumerable<EventBase>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
        Task<EventBase?> AddAsync(EventBase eventBase, CancellationToken cancellationToken = default);
        Task<EventBase?> UpdateAsync(EventBase eventBase, CancellationToken cancellationToken = default);
    }
}
