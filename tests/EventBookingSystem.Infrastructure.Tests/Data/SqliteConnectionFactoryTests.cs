using AwesomeAssertions;
using EventBookingSystem.Infrastructure.Data;

namespace EventBookingSystem.Infrastructure.Tests.Data
{
    [TestClass]
    public class SqliteConnectionFactoryTests
    {
        [TestMethod]
        public void Constructor_WithNullConnectionString_ThrowsArgumentNullException()
        {
            // Arrange
            string connectionString = null!;
            // Act
            Action act = () => new SqliteConnectionFactory(connectionString);
            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void CreateConnection_ReturnsOpenConnection()
        {
            // Arrange
            var factory = new SqliteConnectionFactory("Data Source=:memory:");
            // Act
            using var connection = factory.CreateConnection();
            // Assert
            connection.State.Should().Be(System.Data.ConnectionState.Open);
        }
        [TestMethod]
        public void CreateConnectionAsync_ReturnsOpenConnection()
        {
            // Arrange
            var factory = new SqliteConnectionFactory("Data Source=:memory:");
            // Act
            using var connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            // Assert
            connection.State.Should().Be(System.Data.ConnectionState.Open);
        }
    }
}
