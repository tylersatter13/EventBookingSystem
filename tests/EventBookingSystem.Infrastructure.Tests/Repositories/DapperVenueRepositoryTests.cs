using System.Data;
using AwesomeAssertions;
using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Infrastructure.Data;
using EventBookingSystem.Infrastructure.Repositories;

namespace EventBookingSystem.Infrastructure.Tests.Repositories;

/// <summary>
/// Tests for DapperVenueRepository operations.
/// </summary>
[TestClass]
public class DapperVenueRepositoryTests
{
    #region Constructor Tests

    [TestMethod]
    public void Constructor_WithNullConnectionFactory_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new DapperVenueRepository(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("connectionFactory");
    }

    [TestMethod]
    public void Constructor_WithValidConnectionFactory_CreatesInstance()
    {
        // Arrange
        var connectionFactory = new FakeDBConnectionFactory();

        // Act
        var repository = new DapperVenueRepository(connectionFactory);

        // Assert
        repository.Should().NotBeNull();
    }

    #endregion

    #region AddAsync Tests

    [TestMethod]
    public async Task AddAsync_WithValidVenue_AddsVenueAndReturnsWithId()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=AddAsync_WithValidVenue;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync(); // Keep connection alive
        await InitializeDatabaseAsync(connectionFactory);
        var repository = new DapperVenueRepository(connectionFactory);
        
        var venue = new Venue
        {
            Name = "Test Arena",
            Address = "123 Main Street"
        };

        // Act
        var result = await repository.AddAsync(venue);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(venue, because: "the same instance should be returned");
        result.Id.Should().BeGreaterThan(0, because: "a valid ID should be assigned");
        result.Name.Should().Be("Test Arena");
        result.Address.Should().Be("123 Main Street");
    }

    [TestMethod]
    public async Task AddAsync_WithMultipleVenues_AssignsUniqueIds()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=AddAsync_WithMultipleVenues;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync(); // Keep connection alive
        await InitializeDatabaseAsync(connectionFactory);
        var repository = new DapperVenueRepository(connectionFactory);
        
        var venue1 = new Venue
        {
            Name = "Arena One",
            Address = "100 First Ave"
        };

        var venue2 = new Venue
        {
            Name = "Arena Two",
            Address = "200 Second Ave"
        };

        // Act
        var result1 = await repository.AddAsync(venue1);
        var result2 = await repository.AddAsync(venue2);

        // Assert
        result1.Id.Should().BeGreaterThan(0);
        result2.Id.Should().BeGreaterThan(0);
        result1.Id.Should().NotBe(result2.Id, because: "each venue should have a unique ID");
    }

    [TestMethod]
    public async Task AddAsync_PassesCancellationToken()
    {
        // Arrange
        var trackingFactory = new CancellationTokenTrackingConnectionFactory();
        var repository = new DapperVenueRepository(trackingFactory);
        
        var venue = new Venue
        {
            Name = "Test Venue",
            Address = "Test Address"
        };

        var cancellationToken = new CancellationToken();

        // Act
        try
        {
            await repository.AddAsync(venue, cancellationToken);
        }
        catch
        {
            // Expected to fail since we're using a tracking mock
        }

        // Assert
        trackingFactory.CreateConnectionAsyncWasCalled.Should().BeTrue();
        trackingFactory.ReceivedCancellationToken.Should().Be(cancellationToken);
    }

    [TestMethod]
    public async Task AddAsync_PersistsVenueToDatabase()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=AddAsync_PersistsVenue;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync(); // Keep connection alive
        await InitializeDatabaseAsync(connectionFactory);
        var repository = new DapperVenueRepository(connectionFactory);
        
        var venue = new Venue
        {
            Name = "Persistent Arena",
            Address = "456 Storage Lane"
        };

        // Act
        var addedVenue = await repository.AddAsync(venue);

        // Assert - Verify it was actually saved by reading it back
        var retrievedVenue = await repository.GetByIdAsync(addedVenue.Id);
        retrievedVenue.Should().NotBeNull();
        retrievedVenue!.Name.Should().Be("Persistent Arena");
        retrievedVenue.Address.Should().Be("456 Storage Lane");
    }

    #endregion

    #region GetByIdAsync Tests

    [TestMethod]
    public async Task GetByIdAsync_WhenVenueExists_ReturnsVenue()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=GetByIdAsync_WhenVenueExists;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync(); // Keep connection alive
        await InitializeDatabaseAsync(connectionFactory);
        var repository = new DapperVenueRepository(connectionFactory);

        // Insert test data
        using (var setupConnection = await connectionFactory.CreateConnectionAsync())
        {
            using var command = setupConnection.CreateCommand();
            command.CommandText = "INSERT INTO Venues (Name, Address) VALUES ('Existing Arena', '200 Park Avenue');";
            command.ExecuteNonQuery();
        }

        // Act
        var result = await repository.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Existing Arena");
        result.Address.Should().Be("200 Park Avenue");
    }

    [TestMethod]
    public async Task GetByIdAsync_WhenVenueDoesNotExist_ReturnsNull()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=GetByIdAsync_WhenVenueDoesNotExist;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync(); // Keep connection alive
        await InitializeDatabaseAsync(connectionFactory);
        var repository = new DapperVenueRepository(connectionFactory);

        // Act
        var result = await repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull(because: "no venue exists with ID 999");
    }

    [TestMethod]
    public async Task GetByIdAsync_RetrievesCorrectVenueFromMultiple()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=GetByIdAsync_RetrievesCorrect;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync(); // Keep connection alive
        await InitializeDatabaseAsync(connectionFactory);
        var repository = new DapperVenueRepository(connectionFactory);

        // Insert multiple venues
        using (var setupConnection = await connectionFactory.CreateConnectionAsync())
        {
            using var command = setupConnection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Venues (Name, Address) VALUES ('Arena One', '100 First St');
                INSERT INTO Venues (Name, Address) VALUES ('Arena Two', '200 Second St');
                INSERT INTO Venues (Name, Address) VALUES ('Arena Three', '300 Third St');";
            command.ExecuteNonQuery();
        }

        // Act
        var result = await repository.GetByIdAsync(2);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(2);
        result.Name.Should().Be("Arena Two");
        result.Address.Should().Be("200 Second St");
    }

    [TestMethod]
    public async Task GetByIdAsync_PassesCancellationToken()
    {
        // Arrange
        var trackingFactory = new CancellationTokenTrackingConnectionFactory();
        var repository = new DapperVenueRepository(trackingFactory);
        
        var cancellationToken = new CancellationToken();

        // Act
        try
        {
            await repository.GetByIdAsync(1, cancellationToken);
        }
        catch
        {
            // Expected to fail since we're using a tracking mock
        }

        // Assert
        trackingFactory.CreateConnectionAsyncWasCalled.Should().BeTrue();
        trackingFactory.ReceivedCancellationToken.Should().Be(cancellationToken);
    }

    [TestMethod]
    public async Task GetByIdAsync_MapsAllPropertiesCorrectly()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=GetByIdAsync_MapsAllProperties;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync(); // Keep connection alive
        await InitializeDatabaseAsync(connectionFactory);
        var repository = new DapperVenueRepository(connectionFactory);

        // Insert venue with all properties
        using (var setupConnection = await connectionFactory.CreateConnectionAsync())
        {
            using var command = setupConnection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Venues (Name, Address) 
                VALUES ('Complete Venue', '123 Full Address Street');";
            command.ExecuteNonQuery();
        }

        // Act
        var result = await repository.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Complete Venue");
        result.Address.Should().Be("123 Full Address Street");
    }

    [TestMethod]
    public async Task GetByIdAsync_WithZeroId_ReturnsNull()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=GetByIdAsync_WithZeroId;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync(); // Keep connection alive
        await InitializeDatabaseAsync(connectionFactory);
        var repository = new DapperVenueRepository(connectionFactory);

        // Act
        var result = await repository.GetByIdAsync(0);

        // Assert
        result.Should().BeNull(because: "ID 0 is not a valid venue ID");
    }

    [TestMethod]
    public async Task GetByIdAsync_WithNegativeId_ReturnsNull()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=GetByIdAsync_WithNegativeId;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync(); // Keep connection alive
        await InitializeDatabaseAsync(connectionFactory);
        var repository = new DapperVenueRepository(connectionFactory);

        // Act
        var result = await repository.GetByIdAsync(-1);

        // Assert
        result.Should().BeNull(because: "negative IDs are not valid");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Initializes the database schema for testing using the complete schema file.
    /// </summary>
    private static async Task InitializeDatabaseAsync(IDBConnectionFactory connectionFactory)
    {
        var scriptExecutor = new SqlScriptExecutor(connectionFactory);
        var scriptPath = GetSchemaScriptPath();
        await scriptExecutor.ExecuteScriptFromFileAsync(scriptPath);
    }

    /// <summary>
    /// Gets the path to the CompleteSchema.sql file.
    /// </summary>
    private static string GetSchemaScriptPath()
    {
        var assemblyLocation = Path.GetDirectoryName(typeof(DapperVenueRepositoryTests).Assembly.Location);
        var scriptPath = Path.Combine(assemblyLocation!, "TestData", "CompleteSchema.sql");
        
        if (!File.Exists(scriptPath))
        {
            var projectRoot = Path.GetFullPath(Path.Combine(assemblyLocation!, "..", "..", ".."));
            scriptPath = Path.Combine(projectRoot, "TestData", "CompleteSchema.sql");
        }
        
        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException(
                $"CompleteSchema.sql not found at {scriptPath}. " +
                "Ensure the file is set to 'Copy to Output Directory' in project settings.");
        }
        
        return scriptPath;
    }

    #endregion

    #region Test Doubles

    /// <summary>
    /// Fake connection factory that throws to simulate database unavailability.
    /// </summary>
    private class FakeDBConnectionFactory : IDBConnectionFactory
    {
        public IDbConnection CreateConnection()
        {
            throw new NotImplementedException();
        }

        public Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Connection factory that tracks cancellation token usage.
    /// </summary>
    private class CancellationTokenTrackingConnectionFactory : IDBConnectionFactory
    {
        public bool CreateConnectionAsyncWasCalled { get; private set; }
        public CancellationToken ReceivedCancellationToken { get; private set; }

        public IDbConnection CreateConnection()
        {
            throw new NotImplementedException();
        }

        public Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
        {
            CreateConnectionAsyncWasCalled = true;
            ReceivedCancellationToken = cancellationToken;
            throw new NotImplementedException("This is a tracking fake, not a real implementation");
        }
    }

    #endregion
}
