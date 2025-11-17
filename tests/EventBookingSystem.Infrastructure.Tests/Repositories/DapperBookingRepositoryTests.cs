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
            BookingItems = null
        };

        var addResult = await repository.AddAsync(booking);
        

        // Act
        var result = await repository.GetByIdAsync(addResult.Id);

        // Assert
        result.User.Id.Should().Be(userId);
        result.Event.Id.Should().Be(eventId);
        result.BookingType.Should().Be(BookingType.GA);
        result.PaymentStatus.Should().Be(PaymentStatus.Pending);
        result.TotalAmount.Should().Be(150.00m);
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0, because: "a valid ID should be assigned");

        result.BookingItems.Should().BeEmpty();


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

    #region FindBookingsForPaidUsersAtVenueAsync Tests

    [TestMethod]
    public async Task FindBookingsForPaidUsersAtVenueAsync_ReturnsBookingsForUsersWithPaidBookings()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=FindPaidUsersBookings;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        
        // Setup: Create venue, users, and events
        int venueId, user1Id, user2Id, user3Id, event1Id, event2Id;
        using (var setupConnection = await connectionFactory.CreateConnectionAsync())
        {
            using var command = setupConnection.CreateCommand();
            
            // Create venue
            command.CommandText = "INSERT INTO Venues (Name, Address) VALUES ('Test Venue', '123 Test St'); SELECT last_insert_rowid();";
            venueId = Convert.ToInt32(command.ExecuteScalar());
            
            // Create users
            command.CommandText = "INSERT INTO Users (Name, Email) VALUES ('User 1', 'user1@example.com'); SELECT last_insert_rowid();";
            user1Id = Convert.ToInt32(command.ExecuteScalar());
            
            command.CommandText = "INSERT INTO Users (Name, Email) VALUES ('User 2', 'user2@example.com'); SELECT last_insert_rowid();";
            user2Id = Convert.ToInt32(command.ExecuteScalar());
            
            command.CommandText = "INSERT INTO Users (Name, Email) VALUES ('User 3', 'user3@example.com'); SELECT last_insert_rowid();";
            user3Id = Convert.ToInt32(command.ExecuteScalar());
            
            // Create events at the venue
            command.CommandText = $@"
                INSERT INTO Events (VenueId, Name, StartsAt, EndsAt, EstimatedAttendance, EventType, GA_Capacity)
                VALUES ({venueId}, 'Event 1', '{DateTime.UtcNow:o}', '{DateTime.UtcNow.AddHours(2):o}', 100, 'GeneralAdmission', 100);
                SELECT last_insert_rowid();";
            event1Id = Convert.ToInt32(command.ExecuteScalar());
            
            command.CommandText = $@"
                INSERT INTO Events (VenueId, Name, StartsAt, EndsAt, EstimatedAttendance, EventType, GA_Capacity)
                VALUES ({venueId}, 'Event 2', '{DateTime.UtcNow.AddDays(1):o}', '{DateTime.UtcNow.AddDays(1).AddHours(2):o}', 150, 'GeneralAdmission', 150);
                SELECT last_insert_rowid();";
            event2Id = Convert.ToInt32(command.ExecuteScalar());
            
            // Insert bookings:
            // - User1: Paid booking at venue (should be included)
            // - User1: Pending booking at venue (should be included - same user has paid)
            // - User2: Only pending booking at venue (should be excluded - no paid bookings)
            // - User3: Paid booking at venue (should be included)
            var now = DateTime.UtcNow;
            command.CommandText = $@"
                INSERT INTO Bookings (UserId, EventId, BookingType, PaymentStatus, TotalAmount, CreatedAt)
                VALUES 
                    ({user1Id}, {event1Id}, 'GA', 'Paid', 100.00, '{now.AddHours(-3):o}'),
                    ({user1Id}, {event2Id}, 'GA', 'Pending', 150.00, '{now.AddHours(-2):o}'),
                    ({user2Id}, {event1Id}, 'GA', 'Pending', 75.00, '{now.AddHours(-1):o}'),
                    ({user3Id}, {event2Id}, 'GA', 'Paid', 200.00, '{now:o}');";
            command.ExecuteNonQuery();
        }
        
        var repository = new DapperBookingRepository(connectionFactory);
        
        // Act
        var results = (await repository.FindBookingsForPaidUsersAtVenueAsync(venueId)).ToList();
        
        // Assert
        results.Should().NotBeNull();
        results.Should().HaveCount(3, because: "User1 has 2 bookings (1 paid + 1 pending) and User3 has 1 paid booking");
        
        // Verify User1's bookings are included (both paid and pending)
        results.Count(b => b.User.Id == user1Id).Should().Be(2);
        results.Should().Contain(b => b.User.Id == user1Id && b.PaymentStatus == PaymentStatus.Paid);
        results.Should().Contain(b => b.User.Id == user1Id && b.PaymentStatus == PaymentStatus.Pending);
        
        // Verify User2's booking is NOT included (only has pending)
        results.Should().NotContain(b => b.User.Id == user2Id, because: "User2 has no paid bookings at this venue");
        
        // Verify User3's booking is included
        results.Should().Contain(b => b.User.Id == user3Id && b.PaymentStatus == PaymentStatus.Paid);
    }

    [TestMethod]
    public async Task FindBookingsForPaidUsersAtVenueAsync_ExcludesUsersWithoutPaidBookings()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=FindPaidUsers_NoPaid;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        
        int venueId, userId, eventId;
        using (var setupConnection = await connectionFactory.CreateConnectionAsync())
        {
            using var command = setupConnection.CreateCommand();
            
            // Create venue
            command.CommandText = "INSERT INTO Venues (Name, Address) VALUES ('Test Venue', '123 Test St'); SELECT last_insert_rowid();";
            venueId = Convert.ToInt32(command.ExecuteScalar());
            
            // Create user
            command.CommandText = "INSERT INTO Users (Name, Email) VALUES ('Test User', 'test@example.com'); SELECT last_insert_rowid();";
            userId = Convert.ToInt32(command.ExecuteScalar());
            
            // Create event
            command.CommandText = $@"
                INSERT INTO Events (VenueId, Name, StartsAt, EndsAt, EstimatedAttendance, EventType, GA_Capacity)
                VALUES ({venueId}, 'Test Event', '{DateTime.UtcNow:o}', '{DateTime.UtcNow.AddHours(2):o}', 100, 'GeneralAdmission', 100);
                SELECT last_insert_rowid();";
            eventId = Convert.ToInt32(command.ExecuteScalar());
            
            // Insert only pending/failed bookings (no paid bookings)
            command.CommandText = $@"
                INSERT INTO Bookings (UserId, EventId, BookingType, PaymentStatus, TotalAmount, CreatedAt)
                VALUES 
                    ({userId}, {eventId}, 'GA', 'Pending', 100.00, '{DateTime.UtcNow:o}'),
                    ({userId}, {eventId}, 'GA', 'Failed', 50.00, '{DateTime.UtcNow.AddHours(1):o}');";
            command.ExecuteNonQuery();
        }
        
        var repository = new DapperBookingRepository(connectionFactory);
        
        // Act
        var results = await repository.FindBookingsForPaidUsersAtVenueAsync(venueId);
        
        // Assert
        results.Should().BeEmpty(because: "user has no paid bookings at this venue");
    }

    [TestMethod]
    public async Task FindBookingsForPaidUsersAtVenueAsync_FiltersBookingsByVenue()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=FindPaidUsers_MultiVenue;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        
        int venue1Id, venue2Id, userId, event1Id, event2Id;
        using (var setupConnection = await connectionFactory.CreateConnectionAsync())
        {
            using var command = setupConnection.CreateCommand();
            
            // Create two venues
            command.CommandText = "INSERT INTO Venues (Name, Address) VALUES ('Venue 1', '123 St'); SELECT last_insert_rowid();";
            venue1Id = Convert.ToInt32(command.ExecuteScalar());
            
            command.CommandText = "INSERT INTO Venues (Name, Address) VALUES ('Venue 2', '456 St'); SELECT last_insert_rowid();";
            venue2Id = Convert.ToInt32(command.ExecuteScalar());
            
            command.CommandText = "INSERT INTO Users (Name, Email) VALUES ('User 1', 'user1@example.com'); SELECT last_insert_rowid();";
            userId = Convert.ToInt32(command.ExecuteScalar());
            
            // Create events at different venues
            command.CommandText = $@"
                INSERT INTO Events (VenueId, Name, StartsAt, EndsAt, EstimatedAttendance, EventType, GA_Capacity)
                VALUES ({venue1Id}, 'Event at Venue 1', '{DateTime.UtcNow:o}', '{DateTime.UtcNow.AddHours(2):o}', 100, 'GeneralAdmission', 100);
                SELECT last_insert_rowid();";
            event1Id = Convert.ToInt32(command.ExecuteScalar());
            
            command.CommandText = $@"
                INSERT INTO Events (VenueId, Name, StartsAt, EndsAt, EstimatedAttendance, EventType, GA_Capacity)
                VALUES ({venue2Id}, 'Event at Venue 2', '{DateTime.UtcNow.AddDays(1):o}', '{DateTime.UtcNow.AddDays(1).AddHours(2):o}', 150, 'GeneralAdmission', 150);
                SELECT last_insert_rowid();";
            event2Id = Convert.ToInt32(command.ExecuteScalar());
            
            // User1 has booking at Venue1, User2 has booking at Venue2, User3 has no bookings
            command.CommandText = $@"
                INSERT INTO Bookings (UserId, EventId, BookingType, PaymentStatus, TotalAmount, CreatedAt)
                VALUES ({userId}, {event1Id}, 'GA', 'Paid', 100.00, '{DateTime.UtcNow:o}');
                
                INSERT INTO Bookings (UserId, EventId, BookingType, PaymentStatus, TotalAmount, CreatedAt)
                VALUES ({userId}, {event2Id}, 'GA', 'Pending', 150.00, '{DateTime.UtcNow.AddHours(1):o}');";
            command.ExecuteNonQuery();
        }
        
        var repository = new DapperBookingRepository(connectionFactory);
        
        // Act - Query for Venue1 only
        var results = (await repository.FindBookingsForPaidUsersAtVenueAsync(venue1Id)).ToList();
        
        // Assert
        results.Should().HaveCount(1, because: "User1 has a booking at Venue1");
        results.Should().Contain(b => b.User.Id == userId);
    }

    [TestMethod]
    public async Task FindBookingsForPaidUsersAtVenueAsync_WhenNoBookingsAtVenue_ReturnsEmpty()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=FindPaidUsers_NoBookings;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        
        int venueId;
        using (var setupConnection = await connectionFactory.CreateConnectionAsync())
        {
            using var command = setupConnection.CreateCommand();
            command.CommandText = "INSERT INTO Venues (Name, Address) VALUES ('Empty Venue', '456 Empty St'); SELECT last_insert_rowid();";
            venueId = Convert.ToInt32(command.ExecuteScalar());
        }
        
        var repository = new DapperBookingRepository(connectionFactory);
        
        // Act
        var results = await repository.FindBookingsForPaidUsersAtVenueAsync(venueId);
        
        // Assert
        results.Should().BeEmpty(because: "no bookings exist at this venue");
    }

    [TestMethod]
    public async Task FindBookingsForPaidUsersAtVenueAsync_IncludesAllBookingTypes()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=FindPaidUsers_AllTypes;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        
        int venueId, userId, event1Id, event2Id, event3Id;
        using (var setupConnection = await connectionFactory.CreateConnectionAsync())
        {
            using var command = setupConnection.CreateCommand();
            
            command.CommandText = "INSERT INTO Venues (Name, Address) VALUES ('Multi-Type Venue', '321 Multi St'); SELECT last_insert_rowid();";
            venueId = Convert.ToInt32(command.ExecuteScalar());
            
            command.CommandText = "INSERT INTO Users (Name, Email) VALUES ('User', 'user@example.com'); SELECT last_insert_rowid();";
            userId = Convert.ToInt32(command.ExecuteScalar());
            
            // Create different event types
            command.CommandText = $@"
                INSERT INTO Events (VenueId, Name, StartsAt, EndsAt, EstimatedAttendance, EventType, GA_Capacity)
                VALUES ({venueId}, 'GA Event', '{DateTime.UtcNow:o}', '{DateTime.UtcNow.AddHours(2):o}', 100, 'GeneralAdmission', 100);
                SELECT last_insert_rowid();";
            event1Id = Convert.ToInt32(command.ExecuteScalar());
            
            command.CommandText = $@"
                INSERT INTO Events (VenueId, Name, StartsAt, EndsAt, EstimatedAttendance, EventType)
                VALUES ({venueId}, 'Section Event', '{DateTime.UtcNow.AddDays(1):o}', '{DateTime.UtcNow.AddDays(1).AddHours(2):o}', 200, 'SectionBased');
                SELECT last_insert_rowid();";
            event2Id = Convert.ToInt32(command.ExecuteScalar());
            
            command.CommandText = $@"
                INSERT INTO Events (VenueId, Name, StartsAt, EndsAt, EstimatedAttendance, EventType)
                VALUES ({venueId}, 'Reserved Event', '{DateTime.UtcNow.AddDays(2):o}', '{DateTime.UtcNow.AddDays(2).AddHours(2):o}', 300, 'ReservedSeating');
                SELECT last_insert_rowid();";
            event3Id = Convert.ToInt32(command.ExecuteScalar());
            
            // User has one paid booking (GA type), plus bookings of other types
            command.CommandText = $@"
                INSERT INTO Bookings (UserId, EventId, BookingType, PaymentStatus, TotalAmount, CreatedAt)
                VALUES 
                    ({userId}, {event1Id}, 'GA', 'Paid', 100.00, '{DateTime.UtcNow:o}'),
                    ({userId}, {event2Id}, 'Section', 'Pending', 150.00, '{DateTime.UtcNow.AddHours(1):o}'),
                    ({userId}, {event3Id}, 'Seat', 'Failed', 200.00, '{DateTime.UtcNow.AddHours(2):o}');";
            command.ExecuteNonQuery();
        }
        
        var repository = new DapperBookingRepository(connectionFactory);
        
        // Act
        var results = (await repository.FindBookingsForPaidUsersAtVenueAsync(venueId)).ToList();
        
        // Assert
        results.Should().HaveCount(3, because: "user has paid booking, so all their bookings at venue should be included");
        results.Should().Contain(b => b.BookingType == BookingType.GA && b.PaymentStatus == PaymentStatus.Paid);
        results.Should().Contain(b => b.BookingType == BookingType.Section && b.PaymentStatus == PaymentStatus.Pending);
        results.Should().Contain(b => b.BookingType == BookingType.Seat && b.PaymentStatus == PaymentStatus.Failed);
    }

    [TestMethod]
    public async Task FindBookingsForPaidUsersAtVenueAsync_OrdersByCreatedAtDescending()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=FindPaidUsers_Ordered;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        
        int venueId, userId, eventId;
        using (var setupConnection = await connectionFactory.CreateConnectionAsync())
        {
            using var command = setupConnection.CreateCommand();
            
            command.CommandText = "INSERT INTO Venues (Name, Address) VALUES ('Test Venue', '123 Test St'); SELECT last_insert_rowid();";
            venueId = Convert.ToInt32(command.ExecuteScalar());
            
            command.CommandText = "INSERT INTO Users (Name, Email) VALUES ('User', 'user@example.com'); SELECT last_insert_rowid();";
            userId = Convert.ToInt32(command.ExecuteScalar());
            
            command.CommandText = $@"
                INSERT INTO Events (VenueId, Name, StartsAt, EndsAt, EstimatedAttendance, EventType, GA_Capacity)
                VALUES ({venueId}, 'Test Event', '{DateTime.UtcNow:o}', '{DateTime.UtcNow.AddHours(2):o}', 100, 'GeneralAdmission', 100);
                SELECT last_insert_rowid();";
            eventId = Convert.ToInt32(command.ExecuteScalar());
            
            var now = DateTime.UtcNow;
            command.CommandText = $@"
                INSERT INTO Bookings (UserId, EventId, BookingType, PaymentStatus, TotalAmount, CreatedAt)
                VALUES 
                    ({userId}, {eventId}, 'GA', 'Paid', 100.00, '{now.AddHours(-3):o}'),
                    ({userId}, {eventId}, 'GA', 'Pending', 150.00, '{now:o}'),
                    ({userId}, {eventId}, 'GA', 'Paid', 200.00, '{now.AddHours(-1):o}');";
            command.ExecuteNonQuery();
        }
        
        var repository = new DapperBookingRepository(connectionFactory);
        
        // Act
        var results = (await repository.FindBookingsForPaidUsersAtVenueAsync(venueId)).ToList();
        
        // Assert
        results.Should().HaveCount(3);
        results[0].TotalAmount.Should().Be(150.00m, because: "most recent booking should be first");
        results[1].TotalAmount.Should().Be(200.00m);
        results[2].TotalAmount.Should().Be(100.00m, because: "oldest booking should be last");
    }

    [TestMethod]
    public async Task FindBookingsForPaidUsersAtVenueAsync_LoadsNavigationProperties()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=FindPaidUsers_Navigation;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        
        var (userId, eventId) = await SetupTestDataAsync(connectionFactory);
        
        int venueId;
        using (var setupConnection = await connectionFactory.CreateConnectionAsync())
        {
            using var command = setupConnection.CreateCommand();
            command.CommandText = $"SELECT VenueId FROM Events WHERE Id = {eventId}";
            venueId = Convert.ToInt32(command.ExecuteScalar());
            
            // Insert paid booking
            command.CommandText = $@"
                INSERT INTO Bookings (UserId, EventId, BookingType, PaymentStatus, TotalAmount, CreatedAt)
                VALUES ({userId}, {eventId}, 'GA', 'Paid', 100.00, '{DateTime.UtcNow:o}');";
            command.ExecuteNonQuery();
        }
        
        var repository = new DapperBookingRepository(connectionFactory);
        
        // Act
        var results = (await repository.FindBookingsForPaidUsersAtVenueAsync(venueId)).ToList();
        
        // Assert
        results.Should().HaveCount(1);
        var booking = results.First();
        booking.User.Should().NotBeNull();
        booking.User.Name.Should().Be("Test User");
        booking.Event.Should().NotBeNull();
        booking.Event.Name.Should().Be("Test Event");
        booking.BookingItems.Should().NotBeNull();
    }

    #endregion

    #region FindUsersWithoutBookingsInVenueAsync Tests

    [TestMethod]
    public async Task FindUsersWithoutBookingsInVenueAsync_ReturnsUsersWithNoBookings()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=FindUsersNoBookings;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        
        int venueId, user1Id, user2Id, user3Id, eventId;
        using (var setupConnection = await connectionFactory.CreateConnectionAsync())
        {
            using var command = setupConnection.CreateCommand();
            
            // Create venue
            command.CommandText = "INSERT INTO Venues (Name, Address) VALUES ('Test Venue', '123 Test St'); SELECT last_insert_rowid();";
            venueId = Convert.ToInt32(command.ExecuteScalar());
            
            // Create three users
            command.CommandText = "INSERT INTO Users (Name, Email) VALUES ('User 1', 'user1@example.com'); SELECT last_insert_rowid();";
            user1Id = Convert.ToInt32(command.ExecuteScalar());
            
            command.CommandText = "INSERT INTO Users (Name, Email) VALUES ('User 2', 'user2@example.com'); SELECT last_insert_rowid();";
            user2Id = Convert.ToInt32(command.ExecuteScalar());
            
            command.CommandText = "INSERT INTO Users (Name, Email) VALUES ('User 3', 'user3@example.com'); SELECT last_insert_rowid();";
            user3Id = Convert.ToInt32(command.ExecuteScalar());
            
            // Create event at venue
            command.CommandText = $@"
                INSERT INTO Events (VenueId, Name, StartsAt, EndsAt, EstimatedAttendance, EventType, GA_Capacity)
                VALUES ({venueId}, 'Test Event', '{DateTime.UtcNow:o}', '{DateTime.UtcNow.AddHours(2):o}', 100, 'GeneralAdmission', 100);
                SELECT last_insert_rowid();";
            eventId = Convert.ToInt32(command.ExecuteScalar());
            
            // User1 has a booking, User2 and User3 do not
            command.CommandText = $@"
                INSERT INTO Bookings (UserId, EventId, BookingType, PaymentStatus, TotalAmount, CreatedAt)
                VALUES ({user1Id}, {eventId}, 'GA', 'Paid', 100.00, '{DateTime.UtcNow:o}');";
            command.ExecuteNonQuery();
        }
        
        var repository = new DapperBookingRepository(connectionFactory);
        
        // Act
        var results = (await repository.FindUsersWithoutBookingsInVenueAsync(venueId)).ToList();
        
        // Assert
        results.Should().NotBeNull();
        results.Should().HaveCount(2, because: "User2 and User3 have no bookings at this venue");
        results.Should().Contain(user2Id);
        results.Should().Contain(user3Id);
        results.Should().NotContain(user1Id, because: "User1 has a booking at this venue");
    }

    [TestMethod]
    public async Task FindUsersWithoutBookingsInVenueAsync_AllUsersHaveBookings_ReturnsEmpty()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=FindUsersNoBookings_AllBooked;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        
        int venueId, userId, eventId;
        using (var setupConnection = await connectionFactory.CreateConnectionAsync())
        {
            using var command = setupConnection.CreateCommand();
            
            command.CommandText = "INSERT INTO Venues (Name, Address) VALUES ('Test Venue', '123 Test St'); SELECT last_insert_rowid();";
            venueId = Convert.ToInt32(command.ExecuteScalar());
            
            command.CommandText = "INSERT INTO Users (Name, Email) VALUES ('User', 'user@example.com'); SELECT last_insert_rowid();";
            userId = Convert.ToInt32(command.ExecuteScalar());
            
            command.CommandText = $@"
                INSERT INTO Events (VenueId, Name, StartsAt, EndsAt, EstimatedAttendance, EventType, GA_Capacity)
                VALUES ({venueId}, 'Test Event', '{DateTime.UtcNow:o}', '{DateTime.UtcNow.AddHours(2):o}', 100, 'GeneralAdmission', 100);
                SELECT last_insert_rowid();";
            eventId = Convert.ToInt32(command.ExecuteScalar());
            
            // All users have bookings
            command.CommandText = $@"
                INSERT INTO Bookings (UserId, EventId, BookingType, PaymentStatus, TotalAmount, CreatedAt)
                VALUES ({userId}, {eventId}, 'GA', 'Paid', 100.00, '{DateTime.UtcNow:o}');";
            command.ExecuteNonQuery();
        }
        
        var repository = new DapperBookingRepository(connectionFactory);
        
        // Act
        var results = await repository.FindUsersWithoutBookingsInVenueAsync(venueId);
        
        // Assert
        results.Should().NotBeNull();
        results.Should().BeEmpty(because: "all users have bookings at this venue");
    }

    [TestMethod]
    public async Task FindUsersWithoutBookingsInVenueAsync_NoUsersExist_ReturnsEmpty()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=FindUsersNoBookings_NoUsers;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        
        int venueId;
        using (var setupConnection = await connectionFactory.CreateConnectionAsync())
        {
            using var command = setupConnection.CreateCommand();
            command.CommandText = "INSERT INTO Venues (Name, Address) VALUES ('Empty Venue', '456 Empty St'); SELECT last_insert_rowid();";
            venueId = Convert.ToInt32(command.ExecuteScalar());
        }
        
        var repository = new DapperBookingRepository(connectionFactory);
        
        // Act
        var results = await repository.FindUsersWithoutBookingsInVenueAsync(venueId);
        
        // Assert
        results.Should().NotBeNull();
        results.Should().BeEmpty(because: "no users exist in the database");
    }

    [TestMethod]
    public async Task FindUsersWithoutBookingsInVenueAsync_VenueHasNoEvents_ReturnsAllUsers()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=FindUsersNoBookings_NoEvents;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        
        int venueId, user1Id, user2Id;
        using (var setupConnection = await connectionFactory.CreateConnectionAsync())
        {
            using var command = setupConnection.CreateCommand();
                
            command.CommandText = "INSERT INTO Venues (Name, Address) VALUES ('Empty Venue', '789 Empty St'); SELECT last_insert_rowid();";
            venueId = Convert.ToInt32(command.ExecuteScalar());
                
            command.CommandText = "INSERT INTO Users (Name, Email) VALUES ('User 1', 'user1@example.com'); SELECT last_insert_rowid();";
            user1Id = Convert.ToInt32(command.ExecuteScalar());
                
            command.CommandText = "INSERT INTO Users (Name, Email) VALUES ('User 2', 'user2@example.com'); SELECT last_insert_rowid();";
            user2Id = Convert.ToInt32(command.ExecuteScalar());
        }
        
        var repository = new DapperBookingRepository(connectionFactory);
        
        // Act
        var results = (await repository.FindUsersWithoutBookingsInVenueAsync(venueId)).ToList();
        
        // Assert
        results.Should().HaveCount(2, because: "no events at venue means no users have bookings");
        results.Should().Contain(user1Id);
        results.Should().Contain(user2Id);
    }

    [TestMethod]
    public async Task FindUsersWithoutBookingsInVenueAsync_MultipleVenues_FiltersCorrectly()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=FindUsersNoBookings_MultiVenue;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        
        int venue1Id, venue2Id, user1Id, user2Id, user3Id, event1Id, event2Id;
        using (var setupConnection = await connectionFactory.CreateConnectionAsync())
        {
            using var command = setupConnection.CreateCommand();
                
                // Create two venues
                command.CommandText = "INSERT INTO Venues (Name, Address) VALUES ('Venue 1', '123 St'); SELECT last_insert_rowid();";
                venue1Id = Convert.ToInt32(command.ExecuteScalar());
                
                command.CommandText = "INSERT INTO Venues (Name, Address) VALUES ('Venue 2', '456 St'); SELECT last_insert_rowid();";
                venue2Id = Convert.ToInt32(command.ExecuteScalar());
                
                // Create three users
                command.CommandText = "INSERT INTO Users (Name, Email) VALUES ('User 1', 'user1@example.com'); SELECT last_insert_rowid();";
                user1Id = Convert.ToInt32(command.ExecuteScalar());
                
                command.CommandText = "INSERT INTO Users (Name, Email) VALUES ('User 2', 'user2@example.com'); SELECT last_insert_rowid();";
                user2Id = Convert.ToInt32(command.ExecuteScalar());
                
                command.CommandText = "INSERT INTO Users (Name, Email) VALUES ('User 3', 'user3@example.com'); SELECT last_insert_rowid();";
                user3Id = Convert.ToInt32(command.ExecuteScalar());
                
                // Create events at different venues
                command.CommandText = $@"
                    INSERT INTO Events (VenueId, Name, StartsAt, EndsAt, EstimatedAttendance, EventType, GA_Capacity)
                    VALUES ({venue1Id}, 'Event at Venue 1', '{DateTime.UtcNow:o}', '{DateTime.UtcNow.AddHours(2):o}', 100, 'GeneralAdmission', 100);
                    SELECT last_insert_rowid();";
                event1Id = Convert.ToInt32(command.ExecuteScalar());
                
                command.CommandText = $@"
                    INSERT INTO Events (VenueId, Name, StartsAt, EndsAt, EstimatedAttendance, EventType, GA_Capacity)
                    VALUES ({venue2Id}, 'Event at Venue 2', '{DateTime.UtcNow.AddDays(1):o}', '{DateTime.UtcNow.AddDays(1).AddHours(2):o}', 150, 'GeneralAdmission', 150);
                    SELECT last_insert_rowid();";
                event2Id = Convert.ToInt32(command.ExecuteScalar());
                
                // User1 has booking at Venue1, User2 has booking at Venue2, User3 has no bookings
                command.CommandText = $@"
                    INSERT INTO Bookings (UserId, EventId, BookingType, PaymentStatus, TotalAmount, CreatedAt)
                    VALUES ({user1Id}, {event1Id}, 'GA', 'Paid', 100.00, '{DateTime.UtcNow:o}');
                    
                    INSERT INTO Bookings (UserId, EventId, BookingType, PaymentStatus, TotalAmount, CreatedAt)
                    VALUES ({user2Id}, {event2Id}, 'GA', 'Paid', 150.00, '{DateTime.UtcNow.AddHours(1):o}');";
                command.ExecuteNonQuery();
        }
        
        var repository = new DapperBookingRepository(connectionFactory);
        
        // Act - Query for Venue1
        var results = (await repository.FindUsersWithoutBookingsInVenueAsync(venue1Id)).ToList();
        
        // Assert
        results.Should().HaveCount(2, because: "User2 and User3 have no bookings at Venue1");
        results.Should().Contain(user2Id);
        results.Should().Contain(user3Id);
        results.Should().NotContain(user1Id, because: "User1 has a booking at Venue1");
    }

    [TestMethod]
    public async Task FindUsersWithoutBookingsInVenueAsync_OrdersByUserId()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=FindUsersNoBookings_Ordered;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        
        int venueId, user1Id, user2Id, user3Id;
        using (var setupConnection = await connectionFactory.CreateConnectionAsync())
        {
            using var command = setupConnection.CreateCommand();
                
                command.CommandText = "INSERT INTO Venues (Name, Address) VALUES ('Test Venue', '123 Test St'); SELECT last_insert_rowid();";
                venueId = Convert.ToInt32(command.ExecuteScalar());
                
                // Create users (they will be inserted in order but IDs might vary)
                command.CommandText = "INSERT INTO Users (Name, Email) VALUES ('User 1', 'user1@example.com'); SELECT last_insert_rowid();";
                user1Id = Convert.ToInt32(command.ExecuteScalar());
                
                command.CommandText = "INSERT INTO Users (Name, Email) VALUES ('User 2', 'user2@example.com'); SELECT last_insert_rowid();";
                user2Id = Convert.ToInt32(command.ExecuteScalar());
                
                command.CommandText = "INSERT INTO Users (Name, Email) VALUES ('User 3', 'user3@example.com'); SELECT last_insert_rowid();";
                user3Id = Convert.ToInt32(command.ExecuteScalar());
        }
        
        var repository = new DapperBookingRepository(connectionFactory);
        
        // Act
        var results = (await repository.FindUsersWithoutBookingsInVenueAsync(venueId)).ToList();
        
        // Assert
        results.Should().HaveCount(3);
        results.Should().BeInAscendingOrder(because: "results should be ordered by user ID");
    }

    [TestMethod]
    public async Task FindUsersWithoutBookingsInVenueAsync_IgnoresBookingStatus()
    {
        // Arrange
        var connectionFactory = new SqliteConnectionFactory("Data Source=FindUsersNoBookings_AnyStatus;Mode=Memory;Cache=Shared");
        using var keepAlive = await connectionFactory.CreateConnectionAsync();
        await InitializeDatabaseAsync(connectionFactory);
        
        int venueId, user1Id, user2Id, user3Id, eventId;
        using (var setupConnection = await connectionFactory.CreateConnectionAsync())
        {
            using var command = setupConnection.CreateCommand();
                
                command.CommandText = "INSERT INTO Venues (Name, Address) VALUES ('Test Venue', '123 Test St'); SELECT last_insert_rowid();";
                venueId = Convert.ToInt32(command.ExecuteScalar());
                
                command.CommandText = "INSERT INTO Users (Name, Email) VALUES ('User 1', 'user1@example.com'); SELECT last_insert_rowid();";
                user1Id = Convert.ToInt32(command.ExecuteScalar());
                
                command.CommandText = "INSERT INTO Users (Name, Email) VALUES ('User 2', 'user2@example.com'); SELECT last_insert_rowid();";
                user2Id = Convert.ToInt32(command.ExecuteScalar());
                
                command.CommandText = "INSERT INTO Users (Name, Email) VALUES ('User 3', 'user3@example.com'); SELECT last_insert_rowid();";
                user3Id = Convert.ToInt32(command.ExecuteScalar());
                
                command.CommandText = $@"
                    INSERT INTO Events (VenueId, Name, StartsAt, EndsAt, EstimatedAttendance, EventType, GA_Capacity)
                    VALUES ({venueId}, 'Test Event', '{DateTime.UtcNow:o}', '{DateTime.UtcNow.AddHours(2):o}', 100, 'GeneralAdmission', 100);
                    SELECT last_insert_rowid();";
                eventId = Convert.ToInt32(command.ExecuteScalar());
                
                // User1 has Paid booking, User2 has Failed booking, User3 has no booking
                command.CommandText = $@"
                    INSERT INTO Bookings (UserId, EventId, BookingType, PaymentStatus, TotalAmount, CreatedAt)
                    VALUES ({user1Id}, {eventId}, 'GA', 'Paid', 100.00, '{DateTime.UtcNow:o}');
                    
                    INSERT INTO Bookings (UserId, EventId, BookingType, PaymentStatus, TotalAmount, CreatedAt)
                    VALUES ({user2Id}, {eventId}, 'GA', 'Failed', 50.00, '{DateTime.UtcNow.AddHours(1):o}');";
                command.ExecuteNonQuery();
        }
        
        var repository = new DapperBookingRepository(connectionFactory);
        
        // Act
        var results = (await repository.FindUsersWithoutBookingsInVenueAsync(venueId)).ToList();
        
        // Assert
        results.Should().HaveCount(1, because: "only User3 has no bookings (regardless of status)");
        results.Should().Contain(user3Id);
        results.Should().NotContain(user1Id, because: "User1 has a paid booking");
        results.Should().NotContain(user2Id, because: "User2 has a failed booking (still counts as a booking)");
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
