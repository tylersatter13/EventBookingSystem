using EventBookingSystem.Domain.Entities;

namespace EventBookingSystem.Application.Interfaces
{
    /// <summary>
    /// Repository interface for venue operations.
    /// Defined in Application layer to follow Dependency Inversion Principle.
    /// Implemented in Infrastructure layer.
    /// </summary>
    public interface IVenueRepository
    {
        Task<Venue?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Venue?> GetByIdWithSectionsAsync(int id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Venue>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Venue?> AddAsync(Venue venue, CancellationToken cancellationToken = default);
    }
}
