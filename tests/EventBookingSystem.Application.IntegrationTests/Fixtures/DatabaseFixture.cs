using System.Data;
using Microsoft.Data.Sqlite;
using EventBookingSystem.Infrastructure.Data;
using EventBookingSystem.Application.Interfaces;
using EventBookingSystem.Infrastructure.Repositories;

namespace EventBookingSystem.Application.IntegrationTests.Fixtures
{
    /// <summary>
    /// Provides a shared database fixture for integration tests.
    /// Creates a fresh in-memory SQLite database for each test class.
    /// </summary>
    public class DatabaseFixture : IDisposable
    {
        private readonly string _connectionString;
        private readonly SqliteConnection _keepAliveConnection;
        public IVenueRepository VenueRepository { get; private set; }
        public IEventRepository EventRepository { get; private set; }
        public IUserRepository UserRepository { get; private set; }
        public IBookingRepository BookingRepository { get; private set; }

        public DatabaseFixture()
        {
            // Create in-memory SQLite connection using Microsoft.Data.Sqlite
            // IMPORTANT: Use "Mode=Memory;Cache=Shared" with unique name to keep database alive and isolated per test
            _connectionString = $"Data Source=TestDb_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
            
            // Keep one connection open to prevent the in-memory database from being destroyed
            _keepAliveConnection = new SqliteConnection(_connectionString);
            _keepAliveConnection.Open();

            // Create schema using CompleteSchema.sql
            InitializeSchema();

            // Create repositories with factory that creates new connections
            var connectionFactory = new TestDbConnectionFactory(_connectionString);
            VenueRepository = new DapperVenueRepository(connectionFactory);
            EventRepository = new DapperEventRepository(connectionFactory);
            UserRepository = new DapperUserRepository(connectionFactory);
            BookingRepository = new DapperBookingRepository(connectionFactory);
        }

        private void InitializeSchema()
        {
            var connectionFactory = new TestDbConnectionFactory(_connectionString);
            var scriptExecutor = new SqlScriptExecutor(connectionFactory);
            var scriptPath = GetSchemaScriptPath();
            
            // Execute the complete schema script
            scriptExecutor.ExecuteScriptFromFileAsync(scriptPath).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Gets the path to the CompleteSchema.sql file.
        /// </summary>
        private static string GetSchemaScriptPath()
        {
            var assemblyLocation = Path.GetDirectoryName(typeof(DatabaseFixture).Assembly.Location);
            
            // Try relative to output directory first
            var scriptPath = Path.Combine(assemblyLocation!, "TestData", "CompleteSchema.sql");
            
            if (!File.Exists(scriptPath))
            {
                // Try relative to project root
                var projectRoot = Path.GetFullPath(Path.Combine(assemblyLocation!, "..", "..", ".."));
                scriptPath = Path.Combine(projectRoot, "TestData", "CompleteSchema.sql");
            }
            
            if (!File.Exists(scriptPath))
            {
                // Try going up to the tests directory and then to Infrastructure.Tests
                var testsRoot = Path.GetFullPath(Path.Combine(assemblyLocation!, "..", "..", "..", "..", "EventBookingSystem.Infrastructure.Tests"));
                scriptPath = Path.Combine(testsRoot, "TestData", "CompleteSchema.sql");
            }
            
            if (!File.Exists(scriptPath))
            {
                throw new FileNotFoundException(
                    $"CompleteSchema.sql not found. Searched paths: {scriptPath}. " +
                    "Ensure the file is set to 'Copy to Output Directory' in project settings or is accessible via relative path.");
            }
            
            return scriptPath;
        }

        public void Dispose()
        {
            _keepAliveConnection?.Dispose();
        }

        /// <summary>
        /// Test-specific connection factory that creates new connections for each operation.
        /// </summary>
        private class TestDbConnectionFactory : IDBConnectionFactory
        {
            private readonly string _connectionString;

            public TestDbConnectionFactory(string connectionString)
            {
                _connectionString = connectionString;
            }

            public IDbConnection CreateConnection()
            {
                var connection = new SqliteConnection(_connectionString);
                connection.Open();
                return connection;
            }

            public Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
            {
                var connection = new SqliteConnection(_connectionString);
                connection.Open();
                return Task.FromResult<IDbConnection>(connection);
            }
        }
    }
}
