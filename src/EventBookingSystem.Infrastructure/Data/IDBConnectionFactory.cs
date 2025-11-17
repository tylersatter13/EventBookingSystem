using System.Data;

namespace EventBookingSystem.Infrastructure.Data
{
    /// <summary>
    /// Defines a factory for creating database connections.
    /// </summary>
    public interface IDBConnectionFactory
    {
        /// <summary>
        /// Creates a new database connection synchronously.
        /// </summary>
        /// <returns>A new <see cref="IDbConnection"/> instance.</returns>
        IDbConnection CreateConnection();

        /// <summary>
        /// Creates a new database connection asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a new <see cref="IDbConnection"/> instance.</returns>
        Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
    }
}
