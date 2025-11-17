using EventBookingSystem.Domain.Entities;

namespace EventBookingSystem.Infrastructure.Interfaces
{
    /// <summary>
    /// Repository interface for User entity operations.
    /// </summary>
    public interface IUserRepository : IRespository<User>
    {
        /// <summary>
        /// Gets a user by their email address.
        /// </summary>
        /// <param name="email">The email address to search for.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The user if found, null otherwise.</returns>
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    }
}
