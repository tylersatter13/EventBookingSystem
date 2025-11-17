using Microsoft.Data.Sqlite;
using System.Data;

namespace EventBookingSystem.Infrastructure.Data
{
    /// <inheritdoc/>
    public class SqliteConnectionFactory : IDBConnectionFactory
    {
        private readonly string _connectionString;
        
        public SqliteConnectionFactory(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString)); ;
        }
        /// <inheritdoc/>
        public IDbConnection CreateConnection()
        {
           var connection =  new SqliteConnection(_connectionString);
           connection.Open();
           return connection;
        }

        /// <inheritdoc/>
        public async Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
        {
            var connection =  new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
    }
}
