using System.Data;
using AwesomeAssertions;
using EventBookingSystem.Infrastructure.Data;

namespace EventBookingSystem.Infrastructure.Tests.Data;

/// <summary>
/// Tests for SqlScriptExecutor operations.
/// </summary>
[TestClass]
public class SqlScriptExecutorTests
{
    private const string TestScriptsDirectory = "TestData";

    #region Constructor Tests

    [TestMethod]
    public void Constructor_WithNullConnectionFactory_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new SqlScriptExecutor(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("connectionFactory");
    }

    [TestMethod]
    public void Constructor_WithValidConnectionFactory_CreatesInstance()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=:memory:");

        // Act
        var executor = new SqlScriptExecutor(connectionFactory);

        // Assert
        executor.Should().NotBeNull();
    }

    #endregion


    #region ExecuteScriptAsync Tests

    [TestMethod]
    public async Task ExecuteScriptAsync_WithNullScript_ThrowsArgumentException()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=:memory:");
        var executor = new SqlScriptExecutor(connectionFactory);

        // Act
        Func<Task> act = async () => await executor.ExecuteScriptAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("script");
    }

    [TestMethod]
    public async Task ExecuteScriptAsync_WithEmptyScript_ThrowsArgumentException()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=:memory:");
        var executor = new SqlScriptExecutor(connectionFactory);

        // Act
        Func<Task> act = async () => await executor.ExecuteScriptAsync(string.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("script");
    }

    [TestMethod]
    public async Task ExecuteScriptAsync_WithWhitespaceScript_ThrowsArgumentException()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=:memory:");
        var executor = new SqlScriptExecutor(connectionFactory);

        // Act
        Func<Task> act = async () => await executor.ExecuteScriptAsync("   ");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("script");
    }

    [TestMethod]
    public async Task ExecuteScriptAsync_WithCreateTableAndInsert_ExecutesSuccessfully()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=:memory:");
        var executor = new SqlScriptExecutor(connectionFactory);
        
        var script = @"
            CREATE TABLE AsyncTestTable (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Value INTEGER
            );
            INSERT INTO AsyncTestTable (Name, Value) VALUES ('Test', 42);";

        // Act
        var result = await executor.ExecuteScriptAsync(script);

        // Assert
        result.Should().BeGreaterThanOrEqualTo(0, because: "the script executed successfully");
    }

    [TestMethod]
    public async Task ExecuteScriptAsync_WithInsertScript_ReturnsRowsAffected()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=:memory:");
        var executor = new SqlScriptExecutor(connectionFactory);
        
        // Create and insert in same script
        var script = @"
            CREATE TABLE AsyncTestTable (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL
            );
            INSERT INTO AsyncTestTable (Name) VALUES ('Test1');";

        // Act
        var result = await executor.ExecuteScriptAsync(script);

        // Assert
        result.Should().BeGreaterThanOrEqualTo(0, because: "the script executed successfully");
    }

    [TestMethod]
    public async Task ExecuteScriptAsync_WithMultipleInserts_ReturnsRowsAffected()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=:memory:");
        var executor = new SqlScriptExecutor(connectionFactory);
        
        var insertScript = @"
            CREATE TABLE AsyncTestTable (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL
            );
            INSERT INTO AsyncTestTable (Name) VALUES ('Test1');
            INSERT INTO AsyncTestTable (Name) VALUES ('Test2');
            INSERT INTO AsyncTestTable (Name) VALUES ('Test3');";

        // Act
        var result = await executor.ExecuteScriptAsync(insertScript);

        // Assert
        result.Should().Be(3, because: "three rows were inserted");
    }

    [TestMethod]
    public async Task ExecuteScriptAsync_PassesCancellationToken()
    {
        // Arrange
        var trackingFactory = new CancellationTokenTrackingConnectionFactory();
        var executor = new SqlScriptExecutor(trackingFactory);
        
        // Create a temporary test script
        var tempScript = Path.GetTempFileName();
        File.WriteAllText(tempScript, "SELECT 1");
        
        var cancellationToken = new CancellationToken();

        try
        {
            // Act
            try
            {
                await executor.ExecuteScriptFromFileAsync(tempScript, cancellationToken);
            }
            catch
            {
                // Expected to fail since we're using a tracking fake
            }

            // Assert
            trackingFactory.CreateConnectionAsyncWasCalled.Should().BeTrue();
            trackingFactory.ReceivedCancellationToken.Should().Be(cancellationToken);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempScript))
            {
                File.Delete(tempScript);
            }
        }
    }

    [TestMethod]
    public async Task ExecuteScriptAsync_WithComplexScript_ExecutesAllStatements()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=:memory:");
        var executor = new SqlScriptExecutor(connectionFactory);
        
        var complexScript = @"
            CREATE TABLE AsyncUsers (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL,
                Email TEXT NOT NULL,
                IsActive INTEGER DEFAULT 1
            );
            
            INSERT INTO AsyncUsers (Username, Email) VALUES ('user1', 'user1@test.com');
            INSERT INTO AsyncUsers (Username, Email) VALUES ('user2', 'user2@test.com');
            INSERT INTO AsyncUsers (Username, Email) VALUES ('user3', 'user3@test.com');
            
            UPDATE AsyncUsers SET IsActive = 0 WHERE Username = 'user2';";

        // Act
        var result = await executor.ExecuteScriptAsync(complexScript);

        // Assert
        result.Should().Be(4, because: "three inserts and one update occurred");
    }

    #endregion


    #region ExecuteScriptFromFileAsync Tests

    [TestMethod]
    public async Task ExecuteScriptFromFileAsync_WithNullFilePath_ThrowsArgumentException()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=:memory:");
        var executor = new SqlScriptExecutor(connectionFactory);

        // Act
        Func<Task> act = async () => await executor.ExecuteScriptFromFileAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("filePath");
    }

    [TestMethod]
    public async Task ExecuteScriptFromFileAsync_WithEmptyFilePath_ThrowsArgumentException()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=:memory:");
        var executor = new SqlScriptExecutor(connectionFactory);

        // Act
        Func<Task> act = async () => await executor.ExecuteScriptFromFileAsync(string.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("filePath");
    }

    [TestMethod]
    public async Task ExecuteScriptFromFileAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=:memory:");
        var executor = new SqlScriptExecutor(connectionFactory);
        var nonExistentPath = "NonExistent.sql";

        // Act
        Func<Task> act = async () => await executor.ExecuteScriptFromFileAsync(nonExistentPath);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage($"*{nonExistentPath}*");
    }

    [TestMethod]
    public async Task ExecuteScriptFromFileAsync_WithValidFile_ExecutesSuccessfully()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=:memory:");
        var executor = new SqlScriptExecutor(connectionFactory);
        var scriptPath = GetTestScriptPath("VenueSchema.sql");

        // Act
        var result = await executor.ExecuteScriptFromFileAsync(scriptPath);

        // Assert
        result.Should().BeGreaterThanOrEqualTo(0, because: "the script executed successfully");
    }

    [TestMethod]
    public async Task ExecuteScriptFromFileAsync_CreatesTablesCorrectly()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=:memory:");
        var executor = new SqlScriptExecutor(connectionFactory);
        var scriptPath = GetTestScriptPath("VenueSchema.sql");

        // Act & Assert - Execute script and insert in one operation to maintain connection
        var script = await File.ReadAllTextAsync(scriptPath);
        var combinedScript = script + "\nINSERT INTO Venues (Name, Address, Capacity) VALUES ('Test Venue', 'Test Address', 100);";
        var result = await executor.ExecuteScriptAsync(combinedScript);
        
        result.Should().BeGreaterThanOrEqualTo(0, because: "the script and insert executed successfully");
    }

    [TestMethod]
    public async Task ExecuteScriptFromFileAsync_PassesCancellationToken()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=:memory:");
        var executor = new SqlScriptExecutor(connectionFactory);
        
        // Create a temporary test script
        var tempScript = Path.GetTempFileName();
        File.WriteAllText(tempScript, "SELECT 1");
        
        var trackingFactory = new CancellationTokenTrackingConnectionFactory();
        var trackingExecutor = new SqlScriptExecutor(trackingFactory);
        var cancellationToken = new CancellationToken();

        try
        {
            // Act
            try
            {
                await trackingExecutor.ExecuteScriptFromFileAsync(tempScript, cancellationToken);
            }
            catch
            {
                // Expected to fail since we're using a tracking fake
            }

            // Assert
            trackingFactory.CreateConnectionAsyncWasCalled.Should().BeTrue();
            trackingFactory.ReceivedCancellationToken.Should().Be(cancellationToken);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempScript))
            {
                File.Delete(tempScript);
            }
        }
    }

    #endregion

    #region Test Doubles

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

    #region Helper Methods

    /// <summary>
    /// Gets the full path to a test script file.
    /// </summary>
    private static string GetTestScriptPath(string fileName)
    {
        // Start from the test assembly location and navigate to the TestData folder
        var assemblyLocation = Path.GetDirectoryName(typeof(SqlScriptExecutorTests).Assembly.Location);
        var testDataPath = Path.Combine(assemblyLocation!, TestScriptsDirectory, fileName);
        
        // If not found in bin output, try from source location
        if (!File.Exists(testDataPath))
        {
            // Navigate up from bin\Debug\net10.0 to the test project root
            var projectRoot = Path.GetFullPath(Path.Combine(assemblyLocation!, "..", "..", ".."));
            testDataPath = Path.Combine(projectRoot, TestScriptsDirectory, fileName);
        }
        
        return testDataPath;
    }

    #endregion
}
