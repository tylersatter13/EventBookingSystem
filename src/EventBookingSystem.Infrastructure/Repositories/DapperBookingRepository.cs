using System.Data;
using Dapper;
using EventBookingSystem.Infrastructure.Data;
using EventBookingSystem.Infrastructure.Models;
using EventBookingSystem.Infrastructure.Interfaces;
using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Infrastructure.Mapping;

namespace EventBookingSystem.Infrastructure.Repositories;

/// <summary>
/// Dapper implementation of booking repository using optimized DTOs.
/// </summary>
public class DapperBookingRepository : IBookingRepository
{
    private readonly IDBConnectionFactory _connectionFactory;

    public DapperBookingRepository(IDBConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

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

    public async Task<Booking?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var sql = "SELECT * FROM Bookings WHERE Id = @Id";
        var dto = await connection.QueryFirstOrDefaultAsync<BookingDto>(sql, new { Id = id });

        if (dto == null)
            return null;

        return await PopulateBookingNavigationProperties(connection, dto);
    }

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
    /// Populates navigation properties (User, Event, BookingItems) for a booking.
    /// </summary>
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
