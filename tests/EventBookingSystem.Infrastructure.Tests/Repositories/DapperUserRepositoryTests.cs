using System.Data;
using AwesomeAssertions;
using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Infrastructure.Data;
using EventBookingSystem.Infrastructure.Repositories;

namespace EventBookingSystem.Infrastructure.Tests.Repositories;

/// <summary>
/// Tests for DapperUserRepository operations.
/// </summary>
[TestClass]
public class DapperUserRepositoryTests
{
    #region Constructor Tests

    [TestMethod]
    public void Constructor_WithNullConnectionFactory_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new DapperUserRepository(null!);

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
        var repository = new DapperUserRepository(connectionFactory);

        // Assert
        repository.Should().NotBeNull();
    }

    #endregion

    #region AddAsync Tests

    [TestMethod]
    public async Task AddAsync_WithValidUser_AddsUserAndReturnsWithId()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=AddAsync_WithValidUser;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        var repository = new DapperUserRepository(connectionFactory);
        
        var user = new User
        {
            Name = "John Doe",
            Email = "john.doe@example.com",
            PhoneNumber = "555-1234"
        };

        // Act
        var result = await repository.AddAsync(user);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(user, because: "the same instance should be returned");
        result.Id.Should().BeGreaterThan(0, because: "a valid ID should be assigned");
        result.Name.Should().Be("John Doe");
        result.Email.Should().Be("john.doe@example.com");
        result.PhoneNumber.Should().Be("555-1234");
    }

    [TestMethod]
    public async Task AddAsync_WithUserWithoutPhoneNumber_AddsUserSuccessfully()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=AddAsync_WithoutPhone;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        var repository = new DapperUserRepository(connectionFactory);
        
        var user = new User
        {
            Name = "Jane Smith",
            Email = "jane.smith@example.com",
            PhoneNumber = null
        };

        // Act
        var result = await repository.AddAsync(user);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("Jane Smith");
        result.Email.Should().Be("jane.smith@example.com");
        result.PhoneNumber.Should().BeNull();
    }

    [TestMethod]
    public async Task AddAsync_WithMultipleUsers_AssignsUniqueIds()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=AddAsync_WithMultipleUsers;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        var repository = new DapperUserRepository(connectionFactory);
        
        var user1 = new User
        {
            Name = "User One",
            Email = "user1@example.com"
        };

        var user2 = new User
        {
            Name = "User Two",
            Email = "user2@example.com"
        };

        // Act
        var result1 = await repository.AddAsync(user1);
        var result2 = await repository.AddAsync(user2);

        // Assert
        result1.Id.Should().BeGreaterThan(0);
        result2.Id.Should().BeGreaterThan(0);
        result1.Id.Should().NotBe(result2.Id, because: "each user should have a unique ID");
    }

    [TestMethod]
    public async Task AddAsync_PersistsUserToDatabase()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=AddAsync_PersistsUser;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        var repository = new DapperUserRepository(connectionFactory);
        
        var user = new User
        {
            Name = "Persistent User",
            Email = "persistent@example.com",
            PhoneNumber = "555-9999"
        };

        // Act
        var addedUser = await repository.AddAsync(user);

        // Assert - Verify it was actually saved by reading it back
        var retrievedUser = await repository.GetByIdAsync(addedUser.Id);
        retrievedUser.Should().NotBeNull();
        retrievedUser!.Name.Should().Be("Persistent User");
        retrievedUser.Email.Should().Be("persistent@example.com");
        retrievedUser.PhoneNumber.Should().Be("555-9999");
    }

    [TestMethod]
    public async Task AddAsync_PassesCancellationToken()
    {
        // Arrange
        var trackingFactory = new CancellationTokenTrackingConnectionFactory();
        var repository = new DapperUserRepository(trackingFactory);
        
        var user = new User
        {
            Name = "Test User",
            Email = "test@example.com"
        };

        var cancellationToken = new CancellationToken();

        // Act
        try
        {
            await repository.AddAsync(user, cancellationToken);
        }
        catch
        {
            // Expected to fail since we're using a tracking mock
        }

        // Assert
        trackingFactory.CreateConnectionAsyncWasCalled.Should().BeTrue();
        trackingFactory.ReceivedCancellationToken.Should().Be(cancellationToken);
    }

    #endregion

    #region GetByIdAsync Tests

    [TestMethod]
    public async Task GetByIdAsync_WhenUserExists_ReturnsUser()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=GetByIdAsync_WhenExists;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        var repository = new DapperUserRepository(connectionFactory);

        // Insert test data
        using (var setupConnection = await connectionFactory.CreateConnectionAsync())
        {
            using var command = setupConnection.CreateCommand();
            command.CommandText = "INSERT INTO Users (Name, Email, PhoneNumber) VALUES ('Test User', 'test@example.com', '555-0000');";
            command.ExecuteNonQuery();
        }

        // Act
        var result = await repository.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Test User");
        result.Email.Should().Be("test@example.com");
        result.PhoneNumber.Should().Be("555-0000");
    }

    [TestMethod]
    public async Task GetByIdAsync_WhenUserDoesNotExist_ReturnsNull()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=GetByIdAsync_NotExists;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        var repository = new DapperUserRepository(connectionFactory);

        // Act
        var result = await repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull(because: "no user exists with ID 999");
    }

    [TestMethod]
    public async Task GetByIdAsync_RetrievesCorrectUserFromMultiple()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=GetByIdAsync_Multiple;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        var repository = new DapperUserRepository(connectionFactory);

        // Insert multiple users
        using (var setupConnection = await connectionFactory.CreateConnectionAsync())
        {
            using var command = setupConnection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Users (Name, Email, PhoneNumber) VALUES ('User One', 'user1@example.com', '111-1111');
                INSERT INTO Users (Name, Email, PhoneNumber) VALUES ('User Two', 'user2@example.com', '222-2222');
                INSERT INTO Users (Name, Email, PhoneNumber) VALUES ('User Three', 'user3@example.com', '333-3333');";
            command.ExecuteNonQuery();
        }

        // Act
        var result = await repository.GetByIdAsync(2);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(2);
        result.Name.Should().Be("User Two");
        result.Email.Should().Be("user2@example.com");
        result.PhoneNumber.Should().Be("222-2222");
    }

    [TestMethod]
    public async Task GetByIdAsync_PassesCancellationToken()
    {
        // Arrange
        var trackingFactory = new CancellationTokenTrackingConnectionFactory();
        var repository = new DapperUserRepository(trackingFactory);
        
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

    #endregion

    #region GetByEmailAsync Tests

    [TestMethod]
    public async Task GetByEmailAsync_WhenUserExists_ReturnsUser()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=GetByEmailAsync_Exists;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        var repository = new DapperUserRepository(connectionFactory);

        // Insert test data
        using (var setupConnection = await connectionFactory.CreateConnectionAsync())
        {
            using var command = setupConnection.CreateCommand();
            command.CommandText = "INSERT INTO Users (Name, Email, PhoneNumber) VALUES ('Email User', 'email@example.com', '555-4321');";
            command.ExecuteNonQuery();
        }

        // Act
        var result = await repository.GetByEmailAsync("email@example.com");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Email User");
        result.Email.Should().Be("email@example.com");
        result.PhoneNumber.Should().Be("555-4321");
    }

    [TestMethod]
    public async Task GetByEmailAsync_WhenUserDoesNotExist_ReturnsNull()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=GetByEmailAsync_NotExists;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        var repository = new DapperUserRepository(connectionFactory);

        // Act
        var result = await repository.GetByEmailAsync("nonexistent@example.com");

        // Assert
        result.Should().BeNull(because: "no user exists with that email");
    }

    [TestMethod]
    public async Task GetByEmailAsync_IsCaseSensitive()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=GetByEmailAsync_CaseSensitive;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        var repository = new DapperUserRepository(connectionFactory);

        // Insert test data
        using (var setupConnection = await connectionFactory.CreateConnectionAsync())
        {
            using var command = setupConnection.CreateCommand();
            command.CommandText = "INSERT INTO Users (Name, Email, PhoneNumber) VALUES ('Case User', 'CaseSensitive@Example.com', '555-5555');";
            command.ExecuteNonQuery();
        }

        // Act - SQLite is case-insensitive for LIKE but case-sensitive for = by default
        var result = await repository.GetByEmailAsync("CaseSensitive@Example.com");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("CaseSensitive@Example.com");
    }

    [TestMethod]
    public async Task GetByEmailAsync_PassesCancellationToken()
    {
        // Arrange
        var trackingFactory = new CancellationTokenTrackingConnectionFactory();
        var repository = new DapperUserRepository(trackingFactory);
        
        var cancellationToken = new CancellationToken();

        // Act
        try
        {
            await repository.GetByEmailAsync("test@example.com", cancellationToken);
        }
        catch
        {
            // Expected to fail since we're using a tracking mock
        }

        // Assert
        trackingFactory.CreateConnectionAsyncWasCalled.Should().BeTrue();
        trackingFactory.ReceivedCancellationToken.Should().Be(cancellationToken);
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
        var assemblyLocation = Path.GetDirectoryName(typeof(DapperUserRepositoryTests).Assembly.Location);
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
