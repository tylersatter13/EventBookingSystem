using System.Data;
using AwesomeAssertions;
using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Infrastructure.Data;
using EventBookingSystem.Infrastructure.Interfaces;
using EventBookingSystem.Infrastructure.Repositories;
using EventBookingSystem.Infrastructure.Tests.Helpers;

namespace EventBookingSystem.Infrastructure.Tests.Repositories;

/// <summary>
/// Tests for DapperEventRepository operations.
/// </summary>
[TestClass]
public class DapperEventRepositoryTests
{
    #region Constructor Tests

    [TestMethod]
    public void Constructor_WithNullConnectionFactory_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new DapperEventRepository(null!);

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
        var repository = new DapperEventRepository(connectionFactory);

        // Assert
        repository.Should().NotBeNull();
    }

    #endregion

    #region AddAsync Tests - GeneralAdmissionEvent

    [TestMethod]
    public async Task AddAsync_WithGeneralAdmissionEvent_AddsEventAndReturnsWithId()
    {
        // Arrange
        var connectionFactory = CreateConnectionFactory("AddAsync_GAEvent");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        await InsertTestVenueAsync(connectionFactory, 1, "Test Venue");
        var repository = new DapperEventRepository(connectionFactory);

        var gaEvent = EventTestDataBuilder.CreateGeneralAdmissionEvent(
            name: "Rock Concert",
            venueId: 1,
            capacity: 500);

        // Act
        var result = await repository.AddAsync(gaEvent);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(gaEvent);
        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("Rock Concert");
    }

    [TestMethod]
    public async Task AddAsync_WithGeneralAdmissionEvent_PersistsToDatabase()
    {
        // Arrange
        var connectionFactory = CreateConnectionFactory("AddAsync_GAEvent_Persists");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        await InsertTestVenueAsync(connectionFactory, 1, "Test Venue");
        var repository = new DapperEventRepository(connectionFactory);

        var gaEvent = EventTestDataBuilder.CreateGeneralAdmissionEvent(
            name: "Festival",
            capacity: 1000,
            startsAt: new DateTime(2024, 12, 31, 20, 0, 0));

        // Act
        var addedEvent = await repository.AddAsync(gaEvent);

        // Assert - Verify by reading back
        var retrievedEvent = await repository.GetByIdAsync(addedEvent.Id);
        retrievedEvent.Should().NotBeNull();
        retrievedEvent.Should().BeOfType<GeneralAdmissionEvent>();
        
        var retrievedGa = (GeneralAdmissionEvent)retrievedEvent!;
        retrievedGa.Name.Should().Be("Festival");
        retrievedGa.Capacity.Should().Be(1000);
        retrievedGa.StartsAt.Should().BeCloseTo(gaEvent.StartsAt, TimeSpan.FromSeconds(1));
    }

    #endregion

    #region AddAsync Tests - SectionBasedEvent

    [TestMethod]
    public async Task AddAsync_WithSectionBasedEvent_AddsEventWithSectionInventories()
    {
        // Arrange
        var connectionFactory = CreateConnectionFactory("AddAsync_SBEvent");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        await InsertTestVenueWithSectionsAsync(connectionFactory);
        var repository = new DapperEventRepository(connectionFactory);

        var sbEvent = EventTestDataBuilder.CreateSectionBasedEvent(
            name: "Concert",
            venueId: 1,
            startsAt: null,
            sections: new[] { (1, 500, 100.00m), (2, 300, 75.00m) });

        // Act
        var result = await repository.AddAsync(sbEvent);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        
        var sbResult = (SectionBasedEvent)result;
        sbResult.SectionInventories.Should().HaveCount(2);
        sbResult.SectionInventories.All(si => si.Id > 0).Should().BeTrue();
    }

    [TestMethod]
    public async Task AddAsync_WithSectionBasedEvent_LoadsInventoriesWithGetByIdWithDetails()
    {
        // Arrange
        var connectionFactory = CreateConnectionFactory("AddAsync_SBEvent_LoadDetails");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        await InsertTestVenueWithSectionsAsync(connectionFactory);
        var repository = new DapperEventRepository(connectionFactory);

        var sbEvent = EventTestDataBuilder.CreateSectionBasedEvent(
            name: "Theater Show",
            venueId: 1,
            startsAt: null,
            sections: new[] { (1, 200, 150.00m), (2, 100, 100.00m) });

        var addedEvent = await repository.AddAsync(sbEvent);

        // Act
        var retrievedEvent = await repository.GetByIdWithDetailsAsync(addedEvent.Id);

        // Assert
        retrievedEvent.Should().NotBeNull();
        retrievedEvent.Should().BeOfType<SectionBasedEvent>();
        
        var retrievedSb = (SectionBasedEvent)retrievedEvent!;
        retrievedSb.SectionInventories.Should().HaveCount(2);
        retrievedSb.SectionInventories.Should().Contain(si => si.VenueSectionId == 1 && si.Capacity == 200);
        retrievedSb.SectionInventories.Should().Contain(si => si.VenueSectionId == 2 && si.Capacity == 100);
    }

    #endregion

    #region AddAsync Tests - ReservedSeatingEvent

    [TestMethod]
    public async Task AddAsync_WithReservedSeatingEvent_AddsEventWithSeats()
    {
        // Arrange
        var connectionFactory = CreateConnectionFactory("AddAsync_RSEvent");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        await InsertTestVenueWithSeatsAsync(connectionFactory);
        var repository = new DapperEventRepository(connectionFactory);

        var rsEvent = EventTestDataBuilder.CreateReservedSeatingEvent(
            name: "Play",
            venueId: 1,
            venueSeatIds: [1, 2, 3, 4, 5]);

        // Act
        var result = await repository.AddAsync(rsEvent);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        
        var rsResult = (ReservedSeatingEvent)result;
        rsResult.Seats.Should().HaveCount(5);
        rsResult.Seats.All(s => s.Id > 0).Should().BeTrue();
    }

    [TestMethod]
    public async Task AddAsync_WithReservedSeatingEvent_LoadsSeatsWithGetByIdWithDetails()
    {
        // Arrange
        var connectionFactory = CreateConnectionFactory("AddAsync_RSEvent_LoadDetails");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        await InsertTestVenueWithSeatsAsync(connectionFactory);
        var repository = new DapperEventRepository(connectionFactory);

        var rsEvent = EventTestDataBuilder.CreateReservedSeatingEvent(
            name: "Opera",
            venueId: 1,
            venueSeatIds: [1, 2, 3]);

        var addedEvent = await repository.AddAsync(rsEvent);

        // Act
        var retrievedEvent = await repository.GetByIdWithDetailsAsync(addedEvent.Id);

        // Assert
        retrievedEvent.Should().NotBeNull();
        retrievedEvent.Should().BeOfType<ReservedSeatingEvent>();
        
        var retrievedRs = (ReservedSeatingEvent)retrievedEvent!;
        retrievedRs.Seats.Should().HaveCount(3);
        retrievedRs.Seats.All(s => s.Status == SeatStatus.Available).Should().BeTrue();
    }

    #endregion

    #region GetByIdAsync Tests

    [TestMethod]
    public async Task GetByIdAsync_WhenEventDoesNotExist_ReturnsNull()
    {
        // Arrange
        var connectionFactory = CreateConnectionFactory("GetById_NotExists");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        var repository = new DapperEventRepository(connectionFactory);

        // Act
        var result = await repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetByIdAsync_WithGeneralAdmissionEvent_ReturnsCorrectType()
    {
        // Arrange
        var connectionFactory = CreateConnectionFactory("GetById_GAEvent");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        await InsertTestVenueAsync(connectionFactory, 1, "Test Venue");
        var repository = new DapperEventRepository(connectionFactory);

        var gaEvent = EventTestDataBuilder.CreateGeneralAdmissionEvent();
        await repository.AddAsync(gaEvent);

        // Act
        var result = await repository.GetByIdAsync(gaEvent.Id);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<GeneralAdmissionEvent>();
    }

    #endregion

    #region UpdateAsync Tests

    [TestMethod]
    public async Task UpdateAsync_WithGeneralAdmissionEvent_UpdatesProperties()
    {
        // Arrange
        var connectionFactory = CreateConnectionFactory("UpdateAsync_GAEvent");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        await InsertTestVenueAsync(connectionFactory, 1, "Test Venue");
        var repository = new DapperEventRepository(connectionFactory);

        var gaEvent = EventTestDataBuilder.CreateGeneralAdmissionEvent(name: "Original Name");
        await repository.AddAsync(gaEvent);

        gaEvent.Name = "Updated Name";
        gaEvent.Capacity = 2000;

        // Act
        var result = await repository.UpdateAsync(gaEvent);

        // Assert
        result.Name.Should().Be("Updated Name");
        
        var retrieved = (GeneralAdmissionEvent)(await repository.GetByIdAsync(gaEvent.Id))!;
        retrieved.Name.Should().Be("Updated Name");
        retrieved.Capacity.Should().Be(2000);
    }

    #endregion

    #region DeleteAsync Tests

    [TestMethod]
    public async Task DeleteAsync_WithExistingEvent_DeletesAndReturnsTrue()
    {
        // Arrange
        var connectionFactory = CreateConnectionFactory("DeleteAsync_Exists");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        await InsertTestVenueAsync(connectionFactory, 1, "Test Venue");
        var repository = new DapperEventRepository(connectionFactory);

        var gaEvent = EventTestDataBuilder.CreateGeneralAdmissionEvent();
        await repository.AddAsync(gaEvent);

        // Act
        var result = await repository.DeleteAsync(gaEvent.Id);

        // Assert
        result.Should().BeTrue();
        
        var retrieved = await repository.GetByIdAsync(gaEvent.Id);
        retrieved.Should().BeNull();
    }

    [TestMethod]
    public async Task DeleteAsync_WithNonExistentEvent_ReturnsFalse()
    {
        // Arrange
        var connectionFactory = CreateConnectionFactory("DeleteAsync_NotExists");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        var repository = new DapperEventRepository(connectionFactory);

        // Act
        var result = await repository.DeleteAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetByVenueIdAsync Tests

    [TestMethod]
    public async Task GetByVenueIdAsync_ReturnsAllEventsForVenue()
    {
        // Arrange
        var connectionFactory = CreateConnectionFactory("GetByVenueId");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        await InsertTestVenueAsync(connectionFactory, 1, "Venue 1");
        await InsertTestVenueAsync(connectionFactory, 2, "Venue 2");
        var repository = new DapperEventRepository(connectionFactory);

        await repository.AddAsync(EventTestDataBuilder.CreateGeneralAdmissionEvent("Event 1", venueId: 1));
        await repository.AddAsync(EventTestDataBuilder.CreateGeneralAdmissionEvent("Event 2", venueId: 1));
        await repository.AddAsync(EventTestDataBuilder.CreateGeneralAdmissionEvent("Event 3", venueId: 2));

        // Act
        var results = await repository.GetByVenueIdAsync(1);

        // Assert
        results.Should().HaveCount(2);
        results.All(e => e.VenueId == 1).Should().BeTrue();
    }

    #endregion

    #region GetByDateRangeAsync Tests

    [TestMethod]
    public async Task GetByDateRangeAsync_ReturnsEventsInRange()
    {
        // Arrange
        var connectionFactory = CreateConnectionFactory("GetByDateRange");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        await InsertTestVenueAsync(connectionFactory, 1, "Test Venue");
        var repository = new DapperEventRepository(connectionFactory);

        var date1 = new DateTime(2024, 12, 1, 20, 0, 0);
        var date2 = new DateTime(2024, 12, 15, 20, 0, 0);
        var date3 = new DateTime(2024, 12, 31, 20, 0, 0);

        await repository.AddAsync(EventTestDataBuilder.CreateGeneralAdmissionEvent("Event 1", startsAt: date1));
        await repository.AddAsync(EventTestDataBuilder.CreateGeneralAdmissionEvent("Event 2", startsAt: date2));
        await repository.AddAsync(EventTestDataBuilder.CreateGeneralAdmissionEvent("Event 3", startsAt: date3));

        // Act
        var results = await repository.GetByDateRangeAsync(
            new DateTime(2024, 12, 10),
            new DateTime(2024, 12, 20));

        // Assert
        results.Should().HaveCount(1);
        results.First().Name.Should().Be("Event 2");
    }

    #endregion

    #region SaveSectionInventoriesAsync Tests

    [TestMethod]
    public async Task SaveSectionInventoriesAsync_ReplacesExistingInventories()
    {
        // Arrange
        var connectionFactory = CreateConnectionFactory("SaveInventories");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        await InsertTestVenueWithSectionsAsync(connectionFactory);
        var repository = new DapperEventRepository(connectionFactory);

        var sbEvent = EventTestDataBuilder.CreateSectionBasedEvent(
            venueId: 1,
            sections: (1, 100, 50m));
        await repository.AddAsync(sbEvent);

        var newInventories = new List<EventSectionInventory>
        {
            new() { VenueSectionId = 1, Capacity = 200, Price = 75m },
            new() { VenueSectionId = 2, Capacity = 150, Price = 60m }
        };

        // Act
        await repository.SaveSectionInventoriesAsync(sbEvent.Id, newInventories);

        // Assert
        var retrieved = (SectionBasedEvent)(await repository.GetByIdWithDetailsAsync(sbEvent.Id))!;
        retrieved.SectionInventories.Should().HaveCount(2);
        retrieved.SectionInventories.Should().Contain(si => si.VenueSectionId == 1 && si.Capacity == 200);
    }

    #endregion

    #region Helper Methods

    private static SqliteConnectionFactory CreateConnectionFactory(string dbName)
    {
        return new SqliteConnectionFactory($"Data Source={dbName};Mode=Memory;Cache=Shared");
    }

    private static async Task InitializeDatabaseAsync(IDBConnectionFactory connectionFactory)
    {
        var scriptExecutor = new SqlScriptExecutor(connectionFactory);
        var scriptPath = GetSchemaScriptPath();
        await scriptExecutor.ExecuteScriptFromFileAsync(scriptPath);
    }

    private static string GetSchemaScriptPath()
    {
        var assemblyLocation = Path.GetDirectoryName(typeof(DapperEventRepositoryTests).Assembly.Location);
        var scriptPath = Path.Combine(assemblyLocation!, "TestData", "CompleteSchema.sql");
        
        if (!File.Exists(scriptPath))
        {
            var projectRoot = Path.GetFullPath(Path.Combine(assemblyLocation!, "..", "..", ".."));
            scriptPath = Path.Combine(projectRoot, "TestData", "CompleteSchema.sql");
        }
        
        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException($"CompleteSchema.sql not found at {scriptPath}.");
        }
        
        return scriptPath;
    }

    private static async Task InsertTestVenueAsync(IDBConnectionFactory factory, int id, string name)
    {
        using var connection = await factory.CreateConnectionAsync();
        using var command = connection.CreateCommand();
        command.CommandText = $"INSERT INTO Venues (Id, Name, Address) VALUES ({id}, '{name}', 'Test Address');";
        command.ExecuteNonQuery();
    }

    private static async Task InsertTestVenueWithSectionsAsync(IDBConnectionFactory factory)
    {
        using var connection = await factory.CreateConnectionAsync();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Venues (Id, Name, Address) VALUES (1, 'Test Venue', 'Test Address');
            INSERT INTO VenueSections (Id, VenueId, Name) VALUES (1, 1, 'Orchestra');
            INSERT INTO VenueSections (Id, VenueId, Name) VALUES (2, 1, 'Balcony');";
        command.ExecuteNonQuery();
    }

    private static async Task InsertTestVenueWithSeatsAsync(IDBConnectionFactory factory)
    {
        using var connection = await factory.CreateConnectionAsync();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Venues (Id, Name, Address) VALUES (1, 'Test Venue', 'Test Address');
            INSERT INTO VenueSections (Id, VenueId, Name) VALUES (1, 1, 'Main');
            INSERT INTO VenueSeats (Id, VenueSectionId, Row, SeatNumber) VALUES (1, 1, 'A', '1');
            INSERT INTO VenueSeats (Id, VenueSectionId, Row, SeatNumber) VALUES (2, 1, 'A', '2');
            INSERT INTO VenueSeats (Id, VenueSectionId, Row, SeatNumber) VALUES (3, 1, 'A', '3');
            INSERT INTO VenueSeats (Id, VenueSectionId, Row, SeatNumber) VALUES (4, 1, 'B', '1');
            INSERT INTO VenueSeats (Id, VenueSectionId, Row, SeatNumber) VALUES (5, 1, 'B', '2');";
        command.ExecuteNonQuery();
    }

    #endregion

    #region Test Doubles

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

    #endregion
}
