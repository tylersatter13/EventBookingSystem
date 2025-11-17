using Microsoft.Data.Sqlite;
using System.Data;

namespace EventBookingSystem.Infrastructure.Data
{
    /// <summary>
    /// SQLite implementation of the database connection factory.
    /// Creates and opens SQLite database connections.
    /// </summary>
    public class SqliteConnectionFactory : IDBConnectionFactory
    {
        private readonly string _connectionString;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteConnectionFactory"/> class.
        /// </summary>
        /// <param name="connectionString">The SQLite connection string.</param>
        /// <exception cref="ArgumentNullException">Thrown when connectionString is null.</exception>
        public SqliteConnectionFactory(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        /// <inheritdoc/>
        public IDbConnection CreateConnection()
        {
           var connection = new SqliteConnection(_connectionString);
           connection.Open();
           return connection;
        }

        /// <inheritdoc/>
        public async Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
        {
            var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
    }
}
