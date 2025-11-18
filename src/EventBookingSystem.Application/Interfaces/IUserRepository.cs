using EventBookingSystem.Domain.Entities;

namespace EventBookingSystem.Application.Interfaces
{
    /// <summary>
    /// Repository interface for user operations.
    /// Defined in Application layer to follow Dependency Inversion Principle.
    /// Implemented in Infrastructure layer.
    /// </summary>
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<User?> AddAsync(User user, CancellationToken cancellationToken = default);
    }
}
