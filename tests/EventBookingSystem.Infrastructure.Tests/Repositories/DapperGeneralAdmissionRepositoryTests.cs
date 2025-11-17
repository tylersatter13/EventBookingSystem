using AwesomeAssertions;
using Dapper;
using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Infrastructure.Data;
using EventBookingSystem.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using System.Data;

namespace EventBookingSystem.Infrastructure.Tests.Repositories;

/// <summary>
/// Tests for DapperGeneralAdmissionRepository.
/// Uses in-memory SQLite database for integration testing.
/// </summary>
[TestClass]
public class DapperGeneralAdmissionRepositoryTests
{
    private SqliteConnection _connection = null!;
    private IDBConnectionFactory _connectionFactory = null!;
    private DapperGeneralAdmissionRepository _repository = null!;

    [TestInitialize]
    public async Task Setup()
    {
        // Create in-memory SQLite connection
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        // Create connection factory
        _connectionFactory = new TestConnectionFactory(_connection);

        // Create repository
        _repository = new DapperGeneralAdmissionRepository(_connectionFactory);

        // Create schema
        await CreateSchemaAsync();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Dispose();
    }

    #region AddAsync Tests

    [TestMethod]
    public async Task AddAsync_WithValidGAEvent_InsertsSuccessfully()
    {
        // Arrange
        var venue = await CreateTestVenueAsync();
        var gaEvent = new GeneralAdmissionEvent
        {
            VenueId = venue.Id,
            Name = "Summer Festival",
            StartsAt = DateTime.UtcNow.AddDays(30),
            EndsAt = DateTime.UtcNow.AddDays(30).AddHours(8),
            EstimatedAttendance = 500,
            Capacity = 1000,
            Price = 50.00m
        };

        // Act
        var result = await _repository.AddAsync(gaEvent);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0, because: "ID should be assigned");
        result.Name.Should().Be("Summer Festival");
        result.Capacity.Should().Be(1000);
        result.Attendees.Should().Be(0);
        result.Price.Should().Be(50.00m);
    }

    [TestMethod]
    public async Task AddAsync_WithVenueObject_SetsVenueIdCorrectly()
    {
        // Arrange
        var venue = await CreateTestVenueAsync();
        var gaEvent = new GeneralAdmissionEvent
        {
            Venue = venue,  // Set Venue instead of VenueId
            Name = "Concert",
            StartsAt = DateTime.UtcNow.AddDays(15),
            Capacity = 500,
            Price = 35.00m
        };

        // Act
        var result = await _repository.AddAsync(gaEvent);

        // Assert
        result.VenueId.Should().Be(venue.Id, because: "VenueId should be set from Venue.Id");
        result.Venue.Should().NotBeNull();
        result.Venue!.Id.Should().Be(venue.Id);
    }

    [TestMethod]
    public async Task AddAsync_LoadsVenueNavigationProperty()
    {
        // Arrange
        var venue = await CreateTestVenueAsync();
        var gaEvent = new GeneralAdmissionEvent
        {
            VenueId = venue.Id,
            Name = "Club Night",
            StartsAt = DateTime.UtcNow.AddDays(7),
            Capacity = 300,
            Price = 25.00m
        };

        // Act
        var result = await _repository.AddAsync(gaEvent);

        // Assert
        result.Venue.Should().NotBeNull();
        result.Venue!.Name.Should().Be(venue.Name);
        result.Venue.Address.Should().Be(venue.Address);
    }

    [TestMethod]
    public async Task AddAsync_WithAttendeesReserved_PreservesCount()
    {
        // Arrange
        var venue = await CreateTestVenueAsync();
        var gaEvent = new GeneralAdmissionEvent
        {
            VenueId = venue.Id,
            Name = "Pre-Sold Event",
            StartsAt = DateTime.UtcNow.AddDays(20),
            Capacity = 800
        };
        
        gaEvent.ReserveTickets(150);  // Reserve tickets before saving

        // Act
        var result = await _repository.AddAsync(gaEvent);

        // Assert
        result.Attendees.Should().Be(150);
        result.TotalReserved.Should().Be(150);
        result.AvailableCapacity.Should().Be(650);
    }

    [TestMethod]
    public async Task AddAsync_PersistsToDatabase()
    {
        // Arrange
        var venue = await CreateTestVenueAsync();
        var gaEvent = new GeneralAdmissionEvent
        {
            VenueId = venue.Id,
            Name = "Persistence Test",
            StartsAt = DateTime.UtcNow.AddDays(10),
            Capacity = 200,
            Price = 15.00m
        };

        // Act
        var result = await _repository.AddAsync(gaEvent);

        // Assert - Query database directly to verify
        var sql = "SELECT * FROM Events WHERE Id = @Id AND EventType = 'GeneralAdmission'";
        var fromDb = await _connection.QueryFirstOrDefaultAsync(sql, new { Id = result.Id });
        
        fromDb.Should().NotBeNull();
        ((string)fromDb.Name).Should().Be("Persistence Test");
        ((int)fromDb.GA_Capacity).Should().Be(200);
    }

    #endregion

    #region GetByIdAsync Tests

    [TestMethod]
    public async Task GetByIdAsync_WithExistingGAEvent_ReturnsEvent()
    {
        // Arrange
        var venue = await CreateTestVenueAsync();
        var added = await _repository.AddAsync(new GeneralAdmissionEvent
        {
            VenueId = venue.Id,
            Name = "Test Concert",
            StartsAt = DateTime.UtcNow.AddDays(5),
            Capacity = 500,
            Price = 40.00m
        });

        // Act
        var result = await _repository.GetByIdAsync(added.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(added.Id);
        result.Name.Should().Be("Test Concert");
        result.Capacity.Should().Be(500);
        result.Price.Should().Be(40.00m);
    }

    [TestMethod]
    public async Task GetByIdAsync_LoadsVenueNavigationProperty()
    {
        // Arrange
        var venue = await CreateTestVenueAsync();
        var added = await _repository.AddAsync(new GeneralAdmissionEvent
        {
            VenueId = venue.Id,
            Name = "Venue Test",
            StartsAt = DateTime.UtcNow.AddDays(12),
            Capacity = 600
        });

        // Act
        var result = await _repository.GetByIdAsync(added.Id);

        // Assert
        result!.Venue.Should().NotBeNull();
        result.Venue!.Id.Should().Be(venue.Id);
        result.Venue.Name.Should().Be(venue.Name);
    }

    [TestMethod]
    public async Task GetByIdAsync_RestoresAttendeesCount()
    {
        // Arrange
        var venue = await CreateTestVenueAsync();
        var gaEvent = new GeneralAdmissionEvent
        {
            VenueId = venue.Id,
            Name = "Attendees Test",
            StartsAt = DateTime.UtcNow.AddDays(8),
            Capacity = 1000
        };
        gaEvent.ReserveTickets(250);
        var added = await _repository.AddAsync(gaEvent);

        // Act
        var result = await _repository.GetByIdAsync(added.Id);

        // Assert
        result!.Attendees.Should().Be(250);
        result.TotalReserved.Should().Be(250);
        result.AvailableCapacity.Should().Be(750);
    }

    [TestMethod]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(9999);

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetByIdAsync_WithNonGAEvent_ReturnsNull()
    {
        // Arrange - Insert a SectionBased event
        var venue = await CreateTestVenueAsync();
        var sql = @"
            INSERT INTO Events (VenueId, Name, StartsAt, EventType, EstimatedAttendance)
            VALUES (@VenueId, @Name, @StartsAt, 'SectionBased', 100);
            SELECT last_insert_rowid();";
        
        var sectionEventId = await _connection.ExecuteScalarAsync<int>(sql, new
        {
            VenueId = venue.Id,
            Name = "Section Event",
            StartsAt = DateTime.UtcNow.AddDays(5).ToString("yyyy-MM-dd HH:mm:ss")
        });

        // Act - Try to get as GA event
        var result = await _repository.GetByIdAsync(sectionEventId);

        // Assert
        result.Should().BeNull(because: "query filters by EventType = 'GeneralAdmission'");
    }

    #endregion

    #region UpdateAsync Tests

    [TestMethod]
    public async Task UpdateAsync_WithValidChanges_UpdatesSuccessfully()
    {
        // Arrange
        var venue = await CreateTestVenueAsync();
        var gaEvent = await _repository.AddAsync(new GeneralAdmissionEvent
        {
            VenueId = venue.Id,
            Name = "Original Name",
            StartsAt = DateTime.UtcNow.AddDays(10),
            Capacity = 500,
            Price = 30.00m
        });

        // Modify the event
        gaEvent.Name = "Updated Name";
        gaEvent.Capacity = 600;
        gaEvent.Price = 35.00m;
        gaEvent.ReserveTickets(100);

        // Act
        var result = await _repository.UpdateAsync(gaEvent);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Updated Name");
        result.Capacity.Should().Be(600);
        result.Price.Should().Be(35.00m);
        result.Attendees.Should().Be(100);

        // Verify in database
        var fromDb = await _repository.GetByIdAsync(gaEvent.Id);
        fromDb!.Name.Should().Be("Updated Name");
        fromDb.Capacity.Should().Be(600);
        fromDb.Attendees.Should().Be(100);
    }

    [TestMethod]
    public async Task UpdateAsync_UpdatesAttendeesCount()
    {
        // Arrange
        var venue = await CreateTestVenueAsync();
        var gaEvent = await _repository.AddAsync(new GeneralAdmissionEvent
        {
            VenueId = venue.Id,
            Name = "Attendees Update Test",
            StartsAt = DateTime.UtcNow.AddDays(15),
            Capacity = 1000
        });

        gaEvent.ReserveTickets(150);
        await _repository.UpdateAsync(gaEvent);

        gaEvent.ReserveTickets(50);  // Reserve 50 more

        // Act
        var result = await _repository.UpdateAsync(gaEvent);

        // Assert
        result.Attendees.Should().Be(200);

        // Verify in database
        var fromDb = await _repository.GetByIdAsync(gaEvent.Id);
        fromDb!.Attendees.Should().Be(200);
        fromDb.AvailableCapacity.Should().Be(800);
    }

    [TestMethod]
    public async Task UpdateAsync_WithNonGAEvent_ThrowsArgumentException()
    {
        // Arrange
        var venue = await CreateTestVenueAsync();
        var sectionEvent = new SectionBasedEvent
        {
            VenueId = venue.Id,
            Name = "Section Event",
            StartsAt = DateTime.UtcNow.AddDays(10)
        };

        // Act
        Func<Task> act = async () => await _repository.UpdateAsync(sectionEvent);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*must be a GeneralAdmissionEvent*");
    }

    [TestMethod]
    public async Task UpdateAsync_WithNonExistentId_ThrowsInvalidOperationException()
    {
        // Arrange
        var venue = await CreateTestVenueAsync();
        var gaEvent = new GeneralAdmissionEvent
        {
            Id = 9999,  // Non-existent ID
            VenueId = venue.Id,
            Name = "Ghost Event",
            StartsAt = DateTime.UtcNow.AddDays(5),
            Capacity = 100
        };

        // Act
        Func<Task> act = async () => await _repository.UpdateAsync(gaEvent);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*event with ID 9999 not found*");
    }

    #endregion

    #region Round-Trip Tests

    [TestMethod]
    public async Task RoundTrip_AddGetUpdate_PreservesAllData()
    {
        // Arrange
        var venue = await CreateTestVenueAsync();
        var original = new GeneralAdmissionEvent
        {
            VenueId = venue.Id,
            Name = "Round Trip Test",
            StartsAt = DateTime.UtcNow.AddDays(20),
            EndsAt = DateTime.UtcNow.AddDays(20).AddHours(6),
            EstimatedAttendance = 800,
            Capacity = 1000,
            Price = 45.00m
        };
        original.ReserveTickets(200);

        // Act - Add
        var added = await _repository.AddAsync(original);

        // Act - Get
        var retrieved = await _repository.GetByIdAsync(added.Id);

        // Assert - Verify data preserved
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be(original.Name);
        retrieved.Capacity.Should().Be(original.Capacity);
        retrieved.Attendees.Should().Be(original.Attendees);
        retrieved.Price.Should().Be(original.Price);

        // Act - Update
        retrieved.ReserveTickets(50);
        var updated = await _repository.UpdateAsync(retrieved);

        // Assert - Verify update
        updated.Attendees.Should().Be(250);

        // Act - Get again
        var final = await _repository.GetByIdAsync(added.Id);

        // Assert - Verify final state
        final!.Attendees.Should().Be(250);
        final.AvailableCapacity.Should().Be(750);
    }

    #endregion

    #region Helper Methods

    private async Task CreateSchemaAsync()
    {
        var schema = @"
            CREATE TABLE Venues (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Address TEXT NOT NULL
            );

            CREATE TABLE Events (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                VenueId INTEGER NOT NULL,
                Name TEXT NOT NULL,
                StartsAt TEXT NOT NULL,
                EndsAt TEXT,
                EstimatedAttendance INTEGER NOT NULL,
                EventType TEXT NOT NULL,
                GA_Capacity INTEGER,
                GA_Attendees INTEGER,
                GA_Price REAL,
                GA_CapacityOverride INTEGER,
                SB_CapacityOverride INTEGER,
                FOREIGN KEY (VenueId) REFERENCES Venues(Id)
            );";

        await _connection.ExecuteAsync(schema);
    }

    private async Task<Venue> CreateTestVenueAsync()
    {
        var sql = @"
            INSERT INTO Venues (Name, Address)
            VALUES (@Name, @Address);
            SELECT last_insert_rowid();";

        var id = await _connection.ExecuteScalarAsync<int>(sql, new
        {
            Name = "Test Venue",
            Address = "123 Test Street"
        });

        return new Venue
        {
            Id = id,
            Name = "Test Venue",
            Address = "123 Test Street"
        };
    }

    /// <summary>
    /// Test connection factory that returns the same in-memory connection.
    /// </summary>
    private class TestConnectionFactory : IDBConnectionFactory
    {
        private readonly IDbConnection _connection;

        public TestConnectionFactory(IDbConnection connection)
        {
            _connection = connection;
        }

        public IDbConnection CreateConnection() => _connection;

        public Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_connection);
        }
    }

    #endregion
}
