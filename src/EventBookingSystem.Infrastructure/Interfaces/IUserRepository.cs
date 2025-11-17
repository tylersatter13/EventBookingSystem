using EventBookingSystem.Domain.Entities;

namespace EventBookingSystem.Infrastructure.Interfaces
{
    /// <summary>
    /// Repository interface for <see cref="User"/> entity operations.
    /// </summary>
    public interface IUserRepository : IRepository<User>
    {
        /// <summary>
        /// Gets a user by their email address.
        /// </summary>
        /// <param name="email">The email address to search for.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The <see cref="User"/> if found; otherwise, null.</returns>
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    }
}
