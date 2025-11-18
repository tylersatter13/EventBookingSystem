using System.Data;
using Dapper;
using EventBookingSystem.Infrastructure.Data;
using EventBookingSystem.Infrastructure.Models;
using EventBookingSystem.Application.Interfaces;
using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Infrastructure.Mapping;

namespace EventBookingSystem.Infrastructure.Repositories;

/// <summary>
/// Dapper implementation of booking repository using optimized DTOs.
/// </summary>
public class DapperBookingRepository : IBookingRepository
{
    private readonly IDBConnectionFactory _connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DapperBookingRepository"/> class.
    /// </summary>
    /// <param name="connectionFactory">The database connection factory.</param>
    public DapperBookingRepository(IDBConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    /// <summary>
    /// Adds a new booking entity to the database.
    /// </summary>
    /// <param name="entity">The booking entity to add.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The added <see cref="Booking"/> entity with generated IDs.</returns>
    /// <exception cref="InvalidOperationException">Thrown if booking items are invalid for the booking type.</exception>
    public async Task<Booking> AddAsync(Booking entity, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var dto = BookingMapper.ToDto(entity);

        var sql = @"
            INSERT INTO Bookings (UserId, EventId, BookingType, PaymentStatus, TotalAmount, CreatedAt)
            VALUES (@UserId, @EventId, @BookingType, @PaymentStatus, @TotalAmount, @CreatedAt);
            SELECT last_insert_rowid();";

        dto.Id = await connection.ExecuteScalarAsync<int>(sql, dto);
        entity.Id = dto.Id;

        // Insert booking items only if they exist
        // GA (General Admission) bookings don't have booking items - capacity tracked on event
        // Section bookings require booking items with EventSectionInventoryId set
        // Seat bookings require booking items with EventSeatId set
       if (entity.BookingItems != null && entity.BookingItems.Any())
        {
            foreach (var item in entity.BookingItems)
            {
                var itemDto = BookingItemMapper.ToDto(item);
                itemDto.BookingId = entity.Id;

                // Validate database CHECK constraint: BookingItem must have either EventSeatId OR EventSectionInventoryId
                // If this fails, it means the BookingService incorrectly created BookingItems for a GA booking
                if (itemDto.EventSeatId == null && itemDto.EventSectionInventoryId == null)
                {
                    throw new InvalidOperationException(
                        $"BookingItem must have either EventSeatId or EventSectionInventoryId. " +
                        $"BookingType: {entity.BookingType}. " +
                        $"This indicates a bug in booking creation logic.");
                }

                var itemSql = @"
                    INSERT INTO BookingItems (BookingId, EventSeatId, EventSectionInventoryId, Quantity)
                    VALUES (@BookingId, @EventSeatId, @EventSectionInventoryId, @Quantity);
                    SELECT last_insert_rowid();";

                itemDto.Id = await connection.ExecuteScalarAsync<int>(itemSql, itemDto);
                item.Id = itemDto.Id;
            }
        }

        return entity;
    }

    /// <summary>
    /// Gets a booking by its unique identifier.
    /// </summary>
    /// <param name="id">The booking ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The <see cref="Booking"/> if found; otherwise, null.</returns>
    public async Task<Booking?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var sql = "SELECT * FROM Bookings WHERE Id = @Id";
        var dto = await connection.QueryFirstOrDefaultAsync<BookingDto>(sql, new { Id = id });

        if (dto == null)
            return null;

        return await PopulateBookingNavigationProperties(connection, dto);
    }

    /// <summary>
    /// Gets all bookings for a specific user.
    /// </summary>
    /// <param name="userId">The user ID to search for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A collection of bookings for the user.</returns>
    public async Task<IEnumerable<Booking>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var sql = "SELECT * FROM Bookings WHERE UserId = @UserId ORDER BY CreatedAt DESC";
        var dtos = await connection.QueryAsync<BookingDto>(sql, new { UserId = userId });

        var bookings = new List<Booking>();
        foreach (var dto in dtos)
        {
            var booking = await PopulateBookingNavigationProperties(connection, dto);
            bookings.Add(booking);
        }

        return bookings;
    }

    /// <summary>
    /// Gets all bookings for a specific event.
    /// </summary>
    /// <param name="eventId">The event ID to search for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A collection of bookings for the event.</returns>
    public async Task<IEnumerable<Booking>> GetByEventIdAsync(int eventId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var sql = "SELECT * FROM Bookings WHERE EventId = @EventId ORDER BY CreatedAt DESC";
        var dtos = await connection.QueryAsync<BookingDto>(sql, new { EventId = eventId });

        var bookings = new List<Booking>();
        foreach (var dto in dtos)
        {
            var booking = await PopulateBookingNavigationProperties(connection, dto);
            bookings.Add(booking);
        }

        return bookings;
    }

    /// <summary>
    /// Gets all bookings in the system.
    /// </summary>
    /// <returns>A collection of all bookings.</returns>
    public async Task<IEnumerable<Booking>> GetAllBookings()
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();

        var sql = "SELECT * FROM Bookings ORDER BY CreatedAt DESC";
        var dtos = await connection.QueryAsync<BookingDto>(sql);

        var bookings = new List<Booking>();
        foreach (var dto in dtos)
        {
            var booking = await PopulateBookingNavigationProperties(connection, dto);
            bookings.Add(booking);
        }

        return bookings;
    }

    /// <summary>
    /// Finds bookings for users who have at least one successfully paid booking at the specified venue.
    /// Returns all bookings (paid and unpaid) for qualifying users at the specified venue.
    /// 
    /// Implementation approach:
    /// 1. Find all users who have at least one paid booking at the venue
    /// 2. Return all bookings for those users at the same venue
    /// 
    /// This uses a subquery to identify qualifying users, then retrieves all their bookings at the venue.
    /// </summary>
    /// <param name="venueId">The venue ID to search for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A collection of bookings for users with at least one paid booking at the venue.</returns>
    public async Task<IEnumerable<Booking>> FindBookingsForPaidUsersAtVenueAsync(int venueId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        // SQL query that:
        // 1. Joins Bookings with Events to filter by VenueId
        // 2. Uses a subquery to find users with at least one paid booking at this venue
        // 3. Returns all bookings for those users at this venue
        var sql = @"
            SELECT b.*
            FROM Bookings b
            INNER JOIN Events e ON b.EventId = e.Id
            WHERE e.VenueId = @VenueId
              AND b.UserId IN (
                  SELECT DISTINCT b2.UserId
                  FROM Bookings b2
                  INNER JOIN Events e2 ON b2.EventId = e2.Id
                  WHERE e2.VenueId = @VenueId
                    AND b2.PaymentStatus = 'Paid'
              )
            ORDER BY b.CreatedAt DESC";

        var dtos = await connection.QueryAsync<BookingDto>(sql, new { VenueId = venueId });

        var bookings = new List<Booking>();
        foreach (var dto in dtos)
        {
            var booking = await PopulateBookingNavigationProperties(connection, dto);
            bookings.Add(booking);
        }

        return bookings;
    }

    /// <summary>
    /// Finds all user IDs who have no bookings whatsoever at the specified venue.
    /// 
    /// Implementation approach:
    /// 1. Get all user IDs from the Users table
    /// 2. Exclude users who have any bookings at events in the specified venue
    /// 3. Return the remaining user IDs
    /// 
    /// This is useful for:
    /// - Identifying potential new customers for venue marketing
    /// - Finding users who haven't engaged with a specific venue
    /// - Target marketing campaigns to non-customers
    /// </summary>
    /// <param name="venueId">The venue ID to search for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A collection of user IDs with no bookings at the venue.</returns>
    public async Task<IEnumerable<int>> FindUsersWithoutBookingsInVenueAsync(int venueId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        // SQL query that:
        // 1. Selects all user IDs from Users table
        // 2. Excludes users who appear in any booking for events at this venue
        // 3. Uses NOT IN with subquery to find users without bookings
        var sql = @"
            SELECT u.Id
            FROM Users u
            WHERE u.Id NOT IN (
                SELECT DISTINCT b.UserId
                FROM Bookings b
                INNER JOIN Events e ON b.EventId = e.Id
                WHERE e.VenueId = @VenueId
            )
            ORDER BY u.Id";

        var userIds = await connection.QueryAsync<int>(sql, new { VenueId = venueId });

        return userIds.ToList();
    }

    /// <summary>
    /// Populates navigation properties (User, Event, BookingItems) for a booking.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="dto">The booking DTO.</param>
    /// <returns>The populated <see cref="Booking"/> entity.</returns>
    private async Task<Booking> PopulateBookingNavigationProperties(IDbConnection connection, BookingDto dto)
    {
        var booking = BookingMapper.ToDomain(dto);

        // Load User
        var userSql = "SELECT * FROM Users WHERE Id = @Id";
        var userDto = await connection.QueryFirstOrDefaultAsync<UserDto>(userSql, new { Id = dto.UserId });
        if (userDto != null)
        {
            booking.User = UserMapper.ToDomain(userDto);
        }

        // Load Event  
        var eventSql = "SELECT * FROM Events WHERE Id = @Id";
        var eventDto = await connection.QueryFirstOrDefaultAsync<EventDto>(eventSql, new { Id = dto.EventId });
        if (eventDto != null)
        {
            booking.Event = EventMapper.ToDomain(eventDto);
        }

        // Load booking items
         var itemsSql = "SELECT * FROM BookingItems WHERE BookingId = @BookingId";
         var itemDtos = await connection.QueryAsync<BookingItemDto>(itemsSql, new { BookingId = dto.Id });
         booking.BookingItems = itemDtos.Select(BookingItemMapper.ToDomain).ToList();

        return booking;
    }
}
