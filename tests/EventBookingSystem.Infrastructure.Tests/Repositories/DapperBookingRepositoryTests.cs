using System.Data;
using AwesomeAssertions;
using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Infrastructure.Data;
using EventBookingSystem.Infrastructure.Repositories;

namespace EventBookingSystem.Infrastructure.Tests.Repositories;

/// <summary>
/// Tests for DapperBookingRepository operations.
/// </summary>
[TestClass]
public class DapperBookingRepositoryTests
{
    #region Constructor Tests

    [TestMethod]
    public void Constructor_WithNullConnectionFactory_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new DapperBookingRepository(null!);

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
        var repository = new DapperBookingRepository(connectionFactory);

        // Assert
        repository.Should().NotBeNull();
    }

    #endregion

    #region AddAsync Tests

    [TestMethod]
    public async Task AddAsync_WithValidBooking_AddsBookingAndReturnsWithId()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=AddAsync_ValidBooking;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        var (userId, eventId) = await SetupTestDataAsync(connectionFactory);
        var repository = new DapperBookingRepository(connectionFactory);
        
        var booking = new Booking
        {
            User = new User { Id = userId },
            Event = new GeneralAdmissionEvent { Id = eventId },
            BookingType = BookingType.GA,
            PaymentStatus = PaymentStatus.Pending,
            TotalAmount = 150.00m,
            CreatedAt = DateTime.UtcNow,
            BookingItems = new List<BookingItem>()
        };

        // Act
        var result = await repository.AddAsync(booking);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(booking, because: "the same instance should be returned");
        result.Id.Should().BeGreaterThan(0, because: "a valid ID should be assigned");
        result.BookingType.Should().Be(BookingType.GA);
        result.PaymentStatus.Should().Be(PaymentStatus.Pending);
        result.TotalAmount.Should().Be(150.00m);
    }

    [TestMethod]
    public async Task AddAsync_WithBookingItems_AddsBookingAndItems()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=AddAsync_WithItems;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        var (userId, eventId, sectionInventoryId) = await SetupTestDataWithSectionAsync(connectionFactory);
        var repository = new DapperBookingRepository(connectionFactory);
        
        var booking = new Booking
        {
            User = new User { Id = userId },
            Event = new SectionBasedEvent { Id = eventId },
            BookingType = BookingType.Section,
            PaymentStatus = PaymentStatus.Paid,
            TotalAmount = 200.00m,
            CreatedAt = DateTime.UtcNow,
            BookingItems = new List<BookingItem>
            {
                new BookingItem
                {
                    EventSection = new EventSectionInventory { Id = sectionInventoryId },
                    Quantity = 2
                }
            }
        };

        // Act
        var result = await repository.AddAsync(booking);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.BookingItems.Should().HaveCount(1);
        result.BookingItems.First().Id.Should().BeGreaterThan(0, because: "booking item should have been assigned an ID");
        result.BookingItems.First().Quantity.Should().Be(2);
    }

    [TestMethod]
    public async Task AddAsync_PersistsBookingToDatabase()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=AddAsync_Persists;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        var (userId, eventId) = await SetupTestDataAsync(connectionFactory);
        var repository = new DapperBookingRepository(connectionFactory);
        
        var booking = new Booking
        {
            User = new User { Id = userId },
            Event = new ReservedSeatingEvent { Id = eventId },
            BookingType = BookingType.Seat,
            PaymentStatus = PaymentStatus.Refunded,
            TotalAmount = 75.50m,
            CreatedAt = DateTime.UtcNow,
            BookingItems = new List<BookingItem>()
        };

        // Act
        var addedBooking = await repository.AddAsync(booking);

        // Assert - Verify it was actually saved by reading it back
        var retrievedBooking = await repository.GetByIdAsync(addedBooking.Id);
        retrievedBooking.Should().NotBeNull();
        retrievedBooking!.BookingType.Should().Be(BookingType.Seat);
        retrievedBooking.PaymentStatus.Should().Be(PaymentStatus.Refunded);
        retrievedBooking.TotalAmount.Should().Be(75.50m);
    }

    [TestMethod]
    public async Task AddAsync_PassesCancellationToken()
    {
        // Arrange
        var trackingFactory = new CancellationTokenTrackingConnectionFactory();
        var repository = new DapperBookingRepository(trackingFactory);
        
        var booking = new Booking
        {
            User = new User { Id = 1 },
            Event = new GeneralAdmissionEvent { Id = 1 },
            BookingType = BookingType.GA,
            PaymentStatus = PaymentStatus.Pending,
            TotalAmount = 100.00m,
            CreatedAt = DateTime.UtcNow
        };

        var cancellationToken = new CancellationToken();

        // Act
        try
        {
            await repository.AddAsync(booking, cancellationToken);
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
    public async Task GetByIdAsync_WhenBookingExists_ReturnsBooking()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=GetByIdAsync_Exists;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        var (userId, eventId) = await SetupTestDataAsync(connectionFactory);

        // Insert test booking
        using (var setupConnection = await connectionFactory.CreateConnectionAsync())
        {
            using var command = setupConnection.CreateCommand();
            command.CommandText = $@"
                INSERT INTO Bookings (UserId, EventId, BookingType, PaymentStatus, TotalAmount, CreatedAt)
                VALUES ({userId}, {eventId}, 'GA', 'Paid', 125.00, '{DateTime.UtcNow:o}');";
            command.ExecuteNonQuery();
        }

        var repository = new DapperBookingRepository(connectionFactory);

        // Act
        var result = await repository.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.BookingType.Should().Be(BookingType.GA);
        result.PaymentStatus.Should().Be(PaymentStatus.Paid);
        result.TotalAmount.Should().Be(125.00m);
    }

    [TestMethod]
    public async Task GetByIdAsync_WhenBookingDoesNotExist_ReturnsNull()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=GetByIdAsync_NotExists;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        var repository = new DapperBookingRepository(connectionFactory);

        // Act
        var result = await repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull(because: "no booking exists with ID 999");
    }

    [TestMethod]
    public async Task GetByIdAsync_LoadsBookingItems()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=GetByIdAsync_LoadsItems;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        var (userId, eventId, sectionInventoryId) = await SetupTestDataWithSectionAsync(connectionFactory);

        // Insert test booking with items
        using (var setupConnection = await connectionFactory.CreateConnectionAsync())
        {
            using var command = setupConnection.CreateCommand();
            command.CommandText = $@"
                INSERT INTO Bookings (UserId, EventId, BookingType, PaymentStatus, TotalAmount, CreatedAt)
                VALUES ({userId}, {eventId}, 'Section', 'Paid', 200.00, '{DateTime.UtcNow:o}');
                
                INSERT INTO BookingItems (BookingId, EventSectionInventoryId, Quantity)
                VALUES (1, {sectionInventoryId}, 3);";
            command.ExecuteNonQuery();
        }

        var repository = new DapperBookingRepository(connectionFactory);

        // Act
        var result = await repository.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.BookingItems.Should().NotBeNull();
        result.BookingItems.Should().HaveCount(1);
        result.BookingItems.First().Quantity.Should().Be(3);
    }

    #endregion

    #region GetByUserIdAsync Tests

    [TestMethod]
    public async Task GetByUserIdAsync_ReturnsAllUserBookings()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=GetByUserIdAsync_Multiple;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        var (userId, eventId) = await SetupTestDataAsync(connectionFactory);

        // Insert multiple bookings for the same user
        using (var setupConnection = await connectionFactory.CreateConnectionAsync())
        {
            using var command = setupConnection.CreateCommand();
            command.CommandText = $@"
                INSERT INTO Bookings (UserId, EventId, BookingType, PaymentStatus, TotalAmount, CreatedAt)
                VALUES ({userId}, {eventId}, 'GA', 'Paid', 100.00, '{DateTime.UtcNow:o}');
                
                INSERT INTO Bookings (UserId, EventId, BookingType, PaymentStatus, TotalAmount, CreatedAt)
                VALUES ({userId}, {eventId}, 'Seat', 'Pending', 150.00, '{DateTime.UtcNow.AddHours(1):o}');";
            command.ExecuteNonQuery();
        }

        var repository = new DapperBookingRepository(connectionFactory);

        // Act
        var results = await repository.GetByUserIdAsync(userId);

        // Assert
        results.Should().NotBeNull();
        results.Should().HaveCount(2);
        results.Should().OnlyContain(b => b.BookingItems != null);
    }

    [TestMethod]
    public async Task GetByUserIdAsync_WhenNoBookings_ReturnsEmptyCollection()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=GetByUserIdAsync_Empty;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        var (userId, _) = await SetupTestDataAsync(connectionFactory);
        var repository = new DapperBookingRepository(connectionFactory);

        // Act
        var results = await repository.GetByUserIdAsync(userId);

        // Assert
        results.Should().NotBeNull();
        results.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetByUserIdAsync_OrdersByCreatedAtDescending()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=GetByUserIdAsync_Ordered;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        var (userId, eventId) = await SetupTestDataAsync(connectionFactory);

        var now = DateTime.UtcNow;
        using (var setupConnection = await connectionFactory.CreateConnectionAsync())
        {
            using var command = setupConnection.CreateCommand();
            command.CommandText = $@"
                INSERT INTO Bookings (UserId, EventId, BookingType, PaymentStatus, TotalAmount, CreatedAt)
                VALUES ({userId}, {eventId}, 'GA', 'Paid', 100.00, '{now.AddHours(-2):o}');
                
                INSERT INTO Bookings (UserId, EventId, BookingType, PaymentStatus, TotalAmount, CreatedAt)
                VALUES ({userId}, {eventId}, 'Seat', 'Pending', 150.00, '{now:o}');
                
                INSERT INTO Bookings (UserId, EventId, BookingType, PaymentStatus, TotalAmount, CreatedAt)
                VALUES ({userId}, {eventId}, 'Section', 'Paid', 200.00, '{now.AddHours(-1):o}');";
            command.ExecuteNonQuery();
        }

        var repository = new DapperBookingRepository(connectionFactory);

        // Act
        var results = (await repository.GetByUserIdAsync(userId)).ToList();

        // Assert
        results.Should().HaveCount(3);
        results[0].BookingType.Should().Be(BookingType.Seat, because: "most recent booking should be first");
        results[1].BookingType.Should().Be(BookingType.Section);
        results[2].BookingType.Should().Be(BookingType.GA, because: "oldest booking should be last");
    }

    #endregion

    #region GetByEventIdAsync Tests

    [TestMethod]
    public async Task GetByEventIdAsync_ReturnsAllEventBookings()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=GetByEventIdAsync_Multiple;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        var (userId1, eventId) = await SetupTestDataAsync(connectionFactory);
        
        // Create second user
        int userId2;
        using (var setupConnection = await connectionFactory.CreateConnectionAsync())
        {
            using var command = setupConnection.CreateCommand();
            command.CommandText = "INSERT INTO Users (Name, Email) VALUES ('User Two', 'user2@example.com'); SELECT last_insert_rowid();";
            userId2 = Convert.ToInt32(command.ExecuteScalar());
        }

        // Insert bookings from different users for the same event
        using (var setupConnection = await connectionFactory.CreateConnectionAsync())
        {
            using var command = setupConnection.CreateCommand();
            command.CommandText = $@"
                INSERT INTO Bookings (UserId, EventId, BookingType, PaymentStatus, TotalAmount, CreatedAt)
                VALUES ({userId1}, {eventId}, 'GA', 'Paid', 100.00, '{DateTime.UtcNow:o}');
                
                INSERT INTO Bookings (UserId, EventId, BookingType, PaymentStatus, TotalAmount, CreatedAt)
                VALUES ({userId2}, {eventId}, 'Seat', 'Pending', 150.00, '{DateTime.UtcNow.AddHours(1):o}');";
            command.ExecuteNonQuery();
        }

        var repository = new DapperBookingRepository(connectionFactory);

        // Act
        var results = await repository.GetByEventIdAsync(eventId);

        // Assert
        results.Should().NotBeNull();
        results.Should().HaveCount(2, because: "bookings for both events should be returned");
        // Note: Bookings from different events are returned, but Event details aren't fully loaded
        results.Should().OnlyContain(b => b.BookingItems != null);
    }

    [TestMethod]
    public async Task GetByEventIdAsync_WhenNoBookings_ReturnsEmptyCollection()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=GetByEventIdAsync_Empty;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        var (_, eventId) = await SetupTestDataAsync(connectionFactory);
        var repository = new DapperBookingRepository(connectionFactory);

        // Act
        var results = await repository.GetByEventIdAsync(eventId);

        // Assert
        results.Should().NotBeNull();
        results.Should().BeEmpty();
    }

    #endregion

    #region GetAllBookings Tests

    [TestMethod]
    public async Task GetAllBookings_ReturnsAllBookings()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=GetAllBookings_Multiple;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        var (userId1, eventId) = await SetupTestDataAsync(connectionFactory);
        
        // Create second user
        int userId2;
        using (var setupConnection = await connectionFactory.CreateConnectionAsync())
        {
            using var command = setupConnection.CreateCommand();
            command.CommandText = "INSERT INTO Users (Name, Email) VALUES ('User Two', 'user2@example.com'); SELECT last_insert_rowid();";
            userId2 = Convert.ToInt32(command.ExecuteScalar());
        }

        // Insert bookings from different users
        using (var setupConnection = await connectionFactory.CreateConnectionAsync())
        {
            using var command = setupConnection.CreateCommand();
            command.CommandText = $@"
                INSERT INTO Bookings (UserId, EventId, BookingType, PaymentStatus, TotalAmount, CreatedAt)
                VALUES ({userId1}, {eventId}, 'GA', 'Paid', 100.00, '{DateTime.UtcNow.AddHours(-2):o}');
                
                INSERT INTO Bookings (UserId, EventId, BookingType, PaymentStatus, TotalAmount, CreatedAt)
                VALUES ({userId2}, {eventId}, 'Seat', 'Pending', 150.00, '{DateTime.UtcNow:o}');
                
                INSERT INTO Bookings (UserId, EventId, BookingType, PaymentStatus, TotalAmount, CreatedAt)
                VALUES ({userId1}, {eventId}, 'Section', 'Paid', 200.00, '{DateTime.UtcNow.AddHours(-1):o}');";
            command.ExecuteNonQuery();
        }

        var repository = new DapperBookingRepository(connectionFactory);

        // Act
        var results = await repository.GetAllBookings();

        // Assert
        results.Should().NotBeNull();
        results.Should().HaveCount(3, because: "all bookings in the database should be returned");
        results.Should().OnlyContain(b => b.BookingItems != null, because: "booking items should be loaded for all bookings");
    }

    [TestMethod]
    public async Task GetAllBookings_WhenNoBookings_ReturnsEmptyCollection()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=GetAllBookings_Empty;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        await SetupTestDataAsync(connectionFactory); // Setup users and events but no bookings
        var repository = new DapperBookingRepository(connectionFactory);

        // Act
        var results = await repository.GetAllBookings();

        // Assert
        results.Should().NotBeNull();
        results.Should().BeEmpty(because: "no bookings exist in the database");
    }

    [TestMethod]
    public async Task GetAllBookings_OrdersByCreatedAtDescending()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=GetAllBookings_Ordered;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        var (userId, eventId) = await SetupTestDataAsync(connectionFactory);

        var now = DateTime.UtcNow;
        using (var setupConnection = await connectionFactory.CreateConnectionAsync())
        {
            using var command = setupConnection.CreateCommand();
            command.CommandText = $@"
                INSERT INTO Bookings (UserId, EventId, BookingType, PaymentStatus, TotalAmount, CreatedAt)
                VALUES ({userId}, {eventId}, 'GA', 'Paid', 100.00, '{now.AddHours(-2):o}');
                
                INSERT INTO Bookings (UserId, EventId, BookingType, PaymentStatus, TotalAmount, CreatedAt)
                VALUES ({userId}, {eventId}, 'Seat', 'Pending', 150.00, '{now:o}');
                
                INSERT INTO Bookings (UserId, EventId, BookingType, PaymentStatus, TotalAmount, CreatedAt)
                VALUES ({userId}, {eventId}, 'Section', 'Paid', 200.00, '{now.AddHours(-1):o}');";
            command.ExecuteNonQuery();
        }

        var repository = new DapperBookingRepository(connectionFactory);

        // Act
        var results = (await repository.GetAllBookings()).ToList();

        // Assert
        results.Should().HaveCount(3);
        results[0].BookingType.Should().Be(BookingType.Seat, because: "most recent booking should be first");
        results[1].BookingType.Should().Be(BookingType.Section);
        results[2].BookingType.Should().Be(BookingType.GA, because: "oldest booking should be last");
    }

    [TestMethod]
    public async Task GetAllBookings_LoadsBookingItemsForEachBooking()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=GetAllBookings_WithItems;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        var (userId, eventId, sectionInventoryId) = await SetupTestDataWithSectionAsync(connectionFactory);

        // Insert booking with items
        using (var setupConnection = await connectionFactory.CreateConnectionAsync())
        {
            using var command = setupConnection.CreateCommand();
            command.CommandText = $@"
                INSERT INTO Bookings (UserId, EventId, BookingType, PaymentStatus, TotalAmount, CreatedAt)
                VALUES ({userId}, {eventId}, 'Section', 'Paid', 200.00, '{DateTime.UtcNow:o}');
                
                INSERT INTO BookingItems (BookingId, EventSectionInventoryId, Quantity)
                VALUES (1, {sectionInventoryId}, 3);
                
                INSERT INTO BookingItems (BookingId, EventSectionInventoryId, Quantity)
                VALUES (1, {sectionInventoryId}, 2);";
            command.ExecuteNonQuery();
        }

        var repository = new DapperBookingRepository(connectionFactory);

        // Act
        var results = (await repository.GetAllBookings()).ToList();

        // Assert
        results.Should().HaveCount(1);
        results[0].BookingItems.Should().NotBeNull();
        results[0].BookingItems.Should().HaveCount(2, because: "both booking items should be loaded");
        results[0].BookingItems.Should().Contain(item => item.Quantity == 3);
        results[0].BookingItems.Should().Contain(item => item.Quantity == 2);
    }

    [TestMethod]
    public async Task GetAllBookings_ReturnsBookingsFromMultipleEvents()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=GetAllBookings_MultipleEvents;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        var (userId, eventId1) = await SetupTestDataAsync(connectionFactory);
        
        // Create second event
        int eventId2;
        using (var setupConnection = await connectionFactory.CreateConnectionAsync())
        {
            using var command = setupConnection.CreateCommand();
            command.CommandText = $@"
                SELECT VenueId FROM Events WHERE Id = {eventId1};";
            var venueId = Convert.ToInt32(command.ExecuteScalar());
            
            command.CommandText = $@"
                INSERT INTO Events (VenueId, Name, StartsAt, EndsAt, EstimatedAttendance, EventType, GA_Capacity)
                VALUES ({venueId}, 'Event Two', '{DateTime.UtcNow.AddDays(1):o}', '{DateTime.UtcNow.AddDays(1).AddHours(2):o}', 200, 'GeneralAdmission', 200);
                SELECT last_insert_rowid();";
            eventId2 = Convert.ToInt32(command.ExecuteScalar());
        }

        // Insert bookings for different events
        using (var setupConnection = await connectionFactory.CreateConnectionAsync())
        {
            using var command = setupConnection.CreateCommand();
            command.CommandText = $@"
                INSERT INTO Bookings (UserId, EventId, BookingType, PaymentStatus, TotalAmount, CreatedAt)
                VALUES ({userId}, {eventId1}, 'GA', 'Paid', 100.00, '{DateTime.UtcNow:o}');
                
                INSERT INTO Bookings (UserId, EventId, BookingType, PaymentStatus, TotalAmount, CreatedAt)
                VALUES ({userId}, {eventId2}, 'Seat', 'Pending', 150.00, '{DateTime.UtcNow.AddHours(1):o}');";
            command.ExecuteNonQuery();
        }

        var repository = new DapperBookingRepository(connectionFactory);

        // Act
        var results = (await repository.GetAllBookings()).ToList();

        // Assert
        results.Should().HaveCount(2);
        // Note: Event details aren't fully loaded by GetAllBookings, only IDs are stored
        results.Should().OnlyContain(b => b.BookingItems != null);
    }

    [TestMethod]
    public async Task GetAllBookings_IncludesAllPaymentStatuses()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=GetAllBookings_AllStatuses;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        var (userId, eventId) = await SetupTestDataAsync(connectionFactory);

        // Insert bookings with different payment statuses
        using (var setupConnection = await connectionFactory.CreateConnectionAsync())
        {
            using var command = setupConnection.CreateCommand();
            command.CommandText = $@"
                INSERT INTO Bookings (UserId, EventId, BookingType, PaymentStatus, TotalAmount, CreatedAt)
                VALUES ({userId}, {eventId}, 'GA', 'Pending', 100.00, '{DateTime.UtcNow:o}');
                
                INSERT INTO Bookings (UserId, EventId, BookingType, PaymentStatus, TotalAmount, CreatedAt)
                VALUES ({userId}, {eventId}, 'GA', 'Paid', 150.00, '{DateTime.UtcNow.AddHours(1):o}');
                
                INSERT INTO Bookings (UserId, EventId, BookingType, PaymentStatus, TotalAmount, CreatedAt)
                VALUES ({userId}, {eventId}, 'GA', 'Refunded', 200.00, '{DateTime.UtcNow.AddHours(2):o}');
                
                INSERT INTO Bookings (UserId, EventId, BookingType, PaymentStatus, TotalAmount, CreatedAt)
                VALUES ({userId}, {eventId}, 'GA', 'Failed', 75.00, '{DateTime.UtcNow.AddHours(3):o}');";
            command.ExecuteNonQuery();
        }

        var repository = new DapperBookingRepository(connectionFactory);

        // Act
        var results = (await repository.GetAllBookings()).ToList();

        // Assert
        results.Should().HaveCount(4);
        results.Should().Contain(b => b.PaymentStatus == PaymentStatus.Pending);
        results.Should().Contain(b => b.PaymentStatus == PaymentStatus.Paid);
        results.Should().Contain(b => b.PaymentStatus == PaymentStatus.Refunded);
        results.Should().Contain(b => b.PaymentStatus == PaymentStatus.Failed);
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
    /// Sets up basic test data (user and event).
    /// </summary>
    private static async Task<(int userId, int eventId)> SetupTestDataAsync(IDBConnectionFactory connectionFactory)
    {
        using var connection = await connectionFactory.CreateConnectionAsync();
        using var command = connection.CreateCommand();
        
        // Create venue
        command.CommandText = "INSERT INTO Venues (Name, Address) VALUES ('Test Venue', '123 Test St'); SELECT last_insert_rowid();";
        var venueId = Convert.ToInt32(command.ExecuteScalar());
        
        // Create user
        command.CommandText = "INSERT INTO Users (Name, Email) VALUES ('Test User', 'test@example.com'); SELECT last_insert_rowid();";
        var userId = Convert.ToInt32(command.ExecuteScalar());
        
        // Create event
        command.CommandText = $@"
            INSERT INTO Events (VenueId, Name, StartsAt, EndsAt, EstimatedAttendance, EventType, GA_Capacity)
            VALUES ({venueId}, 'Test Event', '{DateTime.UtcNow:o}', '{DateTime.UtcNow.AddHours(2):o}', 100, 'GeneralAdmission', 100);
            SELECT last_insert_rowid();";
        var eventId = Convert.ToInt32(command.ExecuteScalar());
        
        return (userId, eventId);
    }

    /// <summary>
    /// Sets up test data including a section inventory.
    /// </summary>
    private static async Task<(int userId, int eventId, int sectionInventoryId)> SetupTestDataWithSectionAsync(IDBConnectionFactory connectionFactory)
    {
        var (userId, eventId) = await SetupTestDataAsync(connectionFactory);
        
        using var connection = await connectionFactory.CreateConnectionAsync();
        using var command = connection.CreateCommand();
        
        // Get venue ID from event
        command.CommandText = $"SELECT VenueId FROM Events WHERE Id = {eventId}";
        var venueId = Convert.ToInt32(command.ExecuteScalar());
        
        // Create venue section
        command.CommandText = $"INSERT INTO VenueSections (VenueId, Name) VALUES ({venueId}, 'Test Section'); SELECT last_insert_rowid();";
        var sectionId = Convert.ToInt32(command.ExecuteScalar());
        
        // Create event section inventory
        command.CommandText = $@"
            INSERT INTO EventSectionInventories (EventId, VenueSectionId, Capacity, Price)
            VALUES ({eventId}, {sectionId}, 50, 75.00);
            SELECT last_insert_rowid();";
        var sectionInventoryId = Convert.ToInt32(command.ExecuteScalar());
        
        return (userId, eventId, sectionInventoryId);
    }

    /// <summary>
    /// Gets the path to the CompleteSchema.sql file.
    /// </summary>
    private static string GetSchemaScriptPath()
    {
        var assemblyLocation = Path.GetDirectoryName(typeof(DapperBookingRepositoryTests).Assembly.Location);
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
