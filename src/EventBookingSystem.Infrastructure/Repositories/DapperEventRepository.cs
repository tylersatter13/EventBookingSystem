using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Infrastructure.Data;
using EventBookingSystem.Infrastructure.Interfaces;
using EventBookingSystem.Infrastructure.Mapping;
using EventBookingSystem.Infrastructure.Models;

namespace EventBookingSystem.Infrastructure.Repositories;

/// <summary>
/// Dapper implementation of event repository supporting polymorphic event types.
/// Uses Table Per Hierarchy (TPH) pattern with discriminator column.
/// </summary>
public class DapperEventRepository : IEventRepository
{
    private readonly IDBConnectionFactory _connectionFactory;

    public DapperEventRepository(IDBConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    #region Basic CRUD Operations

    public async Task<EventBase> AddAsync(EventBase entity, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        try
        {
            var dto = EventMapper.ToDto(entity);

            // Insert the main event record
            var sql = BuildInsertSql(dto.EventType);
            dto.Id = await connection.ExecuteScalarAsync<int>(sql, dto, transaction);
            entity.Id = dto.Id;

            // Save related data based on event type
            await SaveRelatedDataAsync(connection, transaction, entity);

            transaction.Commit();
            return entity;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<EventBase?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var sql = "SELECT * FROM Events WHERE Id = @Id";
        var dto = await connection.QueryFirstOrDefaultAsync<EventDto>(sql, new { Id = id });

        return dto != null ? EventMapper.ToDomain(dto) : null;
    }

    public async Task<EventBase?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var sql = "SELECT * FROM Events WHERE Id = @Id";
        var dto = await connection.QueryFirstOrDefaultAsync<EventDto>(sql, new { Id = id });

        if (dto == null)
            return null;

        var eventBase = EventMapper.ToDomain(dto);

        // Load related data based on event type
        await LoadRelatedDataAsync(connection, eventBase);

        return eventBase;
    }

    public async Task<EventBase> UpdateAsync(EventBase entity, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        try
        {
            var dto = EventMapper.ToDto(entity);

            // Update the main event record
            var sql = BuildUpdateSql(dto.EventType);
            await connection.ExecuteAsync(sql, dto, transaction);

            // Update related data based on event type
            await UpdateRelatedDataAsync(connection, transaction, entity);

            transaction.Commit();
            return entity;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var sql = "DELETE FROM Events WHERE Id = @Id";
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });

        return rowsAffected > 0;
    }

    #endregion

    #region Query Operations

    public async Task<IEnumerable<EventBase>> GetByVenueIdAsync(int venueId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var sql = "SELECT * FROM Events WHERE VenueId = @VenueId ORDER BY StartsAt";
        var dtos = await connection.QueryAsync<EventDto>(sql, new { VenueId = venueId });

        return dtos.Select(EventMapper.ToDomain).ToList();
    }

    public async Task<IEnumerable<EventBase>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var sql = @"
            SELECT * FROM Events 
            WHERE StartsAt >= @StartDate AND StartsAt <= @EndDate
            ORDER BY StartsAt";

        var dtos = await connection.QueryAsync<EventDto>(sql, new 
        { 
            StartDate = startDate.ToString("yyyy-MM-dd HH:mm:ss"), 
            EndDate = endDate.ToString("yyyy-MM-dd HH:mm:ss") 
        });

        return dtos.Select(EventMapper.ToDomain).ToList();
    }

    #endregion

    #region Section Inventories and Event Seats

    public async Task SaveSectionInventoriesAsync(int eventId, IEnumerable<EventSectionInventory> inventories, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        try
        {
            // Delete existing inventories
            var deleteSql = "DELETE FROM EventSectionInventories WHERE EventId = @EventId";
            await connection.ExecuteAsync(deleteSql, new { EventId = eventId }, transaction);

            // Insert new inventories
            var insertSql = @"
                INSERT INTO EventSectionInventories 
                (EventId, VenueSectionId, Capacity, Booked, Price, AllocationMode)
                VALUES 
                (@EventId, @VenueSectionId, @Capacity, @Booked, @Price, @AllocationMode)";

            foreach (var inventory in inventories)
            {
                var dto = EventSectionInventoryMapper.ToDto(inventory);
                dto.EventId = eventId; // Set the EventId
                await connection.ExecuteAsync(insertSql, dto, transaction);
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task SaveEventSeatsAsync(int eventId, IEnumerable<EventSeat> seats, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        try
        {
            // Delete existing seats
            var deleteSql = "DELETE FROM EventSeats WHERE EventId = @EventId";
            await connection.ExecuteAsync(deleteSql, new { EventId = eventId }, transaction);

            // Insert new seats
            var insertSql = @"
                INSERT INTO EventSeats 
                (EventId, VenueSeatId, Status)
                VALUES 
                (@EventId, @VenueSeatId, @Status)";

            foreach (var seat in seats)
            {
                var dto = EventSeatMapper.ToDto(seat);
                dto.EventId = eventId; // Set the EventId
                await connection.ExecuteAsync(insertSql, dto, transaction);
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    #endregion

    #region Helper Methods

    private static string BuildInsertSql(string eventType)
    {
        return eventType switch
        {
            "GeneralAdmission" => @"
                INSERT INTO Events 
                (VenueId, Name, StartsAt, EndsAt, EstimatedAttendance, EventType, 
                 GA_Capacity, GA_Attendees, GA_Price, GA_CapacityOverride)
                VALUES 
                (@VenueId, @Name, @StartsAt, @EndsAt, @EstimatedAttendance, @EventType,
                 @GA_Capacity, @GA_Attendees, @GA_Price, @GA_CapacityOverride);
                SELECT last_insert_rowid();",

            "SectionBased" => @"
                INSERT INTO Events 
                (VenueId, Name, StartsAt, EndsAt, EstimatedAttendance, EventType, SB_CapacityOverride)
                VALUES 
                (@VenueId, @Name, @StartsAt, @EndsAt, @EstimatedAttendance, @EventType, @SB_CapacityOverride);
                SELECT last_insert_rowid();",

            "ReservedSeating" => @"
                INSERT INTO Events 
                (VenueId, Name, StartsAt, EndsAt, EstimatedAttendance, EventType)
                VALUES 
                (@VenueId, @Name, @StartsAt, @EndsAt, @EstimatedAttendance, @EventType);
                SELECT last_insert_rowid();",

            _ => throw new InvalidOperationException($"Unknown event type: {eventType}")
        };
    }

    private static string BuildUpdateSql(string eventType)
    {
        return eventType switch
        {
            "GeneralAdmission" => @"
                UPDATE Events SET
                    VenueId = @VenueId,
                    Name = @Name,
                    StartsAt = @StartsAt,
                    EndsAt = @EndsAt,
                    EstimatedAttendance = @EstimatedAttendance,
                    GA_Capacity = @GA_Capacity,
                    GA_Attendees = @GA_Attendees,
                    GA_Price = @GA_Price,
                    GA_CapacityOverride = @GA_CapacityOverride
                WHERE Id = @Id",

            "SectionBased" => @"
                UPDATE Events SET
                    VenueId = @VenueId,
                    Name = @Name,
                    StartsAt = @StartsAt,
                    EndsAt = @EndsAt,
                    EstimatedAttendance = @EstimatedAttendance,
                    SB_CapacityOverride = @SB_CapacityOverride
                WHERE Id = @Id",

            "ReservedSeating" => @"
                UPDATE Events SET
                    VenueId = @VenueId,
                    Name = @Name,
                    StartsAt = @StartsAt,
                    EndsAt = @EndsAt,
                    EstimatedAttendance = @EstimatedAttendance
                WHERE Id = @Id",

            _ => throw new InvalidOperationException($"Unknown event type: {eventType}")
        };
    }

    private async Task SaveRelatedDataAsync(IDbConnection connection, IDbTransaction transaction, EventBase eventBase)
    {
        switch (eventBase)
        {
            case SectionBasedEvent sbEvent when sbEvent.SectionInventories.Any():
                await SaveSectionInventoriesInternalAsync(connection, transaction, sbEvent.Id, sbEvent.SectionInventories);
                break;

            case ReservedSeatingEvent rsEvent when rsEvent.Seats.Any():
                await SaveEventSeatsInternalAsync(connection, transaction, rsEvent.Id, rsEvent.Seats);
                break;
        }
    }

    private async Task UpdateRelatedDataAsync(IDbConnection connection, IDbTransaction transaction, EventBase eventBase)
    {
        // Same as SaveRelatedDataAsync since we replace all related data
        await SaveRelatedDataAsync(connection, transaction, eventBase);
    }

    private async Task LoadRelatedDataAsync(IDbConnection connection, EventBase eventBase)
    {
        switch (eventBase)
        {
            case SectionBasedEvent sbEvent:
                await LoadSectionInventoriesAsync(connection, sbEvent);
                break;

            case ReservedSeatingEvent rsEvent:
                await LoadEventSeatsAsync(connection, rsEvent);
                break;
        }
    }

    private async Task LoadSectionInventoriesAsync(IDbConnection connection, SectionBasedEvent sbEvent)
    {
        var sql = "SELECT * FROM EventSectionInventories WHERE EventId = @EventId";
        var dtos = await connection.QueryAsync<EventSectionInventoryDto>(sql, new { EventId = sbEvent.Id });

        sbEvent.SectionInventories = dtos
            .Select(EventSectionInventoryMapper.ToDomain)
            .ToList();
    }

    private async Task LoadEventSeatsAsync(IDbConnection connection, ReservedSeatingEvent rsEvent)
    {
        var sql = "SELECT * FROM EventSeats WHERE EventId = @EventId";
        var dtos = await connection.QueryAsync<EventSeatDto>(sql, new { EventId = rsEvent.Id });

        rsEvent.Seats = dtos
            .Select(EventSeatMapper.ToDomain)
            .ToList();
    }

    private async Task SaveSectionInventoriesInternalAsync(
        IDbConnection connection, 
        IDbTransaction transaction, 
        int eventId, 
        IEnumerable<EventSectionInventory> inventories)
    {
        var insertSql = @"
            INSERT INTO EventSectionInventories 
            (EventId, VenueSectionId, Capacity, Booked, Price, AllocationMode)
            VALUES 
            (@EventId, @VenueSectionId, @Capacity, @Booked, @Price, @AllocationMode);
            SELECT last_insert_rowid();";

        foreach (var inventory in inventories)
        {
            var dto = EventSectionInventoryMapper.ToDto(inventory);
            dto.EventId = eventId;
            dto.Id = await connection.ExecuteScalarAsync<int>(insertSql, dto, transaction);
            inventory.Id = dto.Id;
        }
    }

    private async Task SaveEventSeatsInternalAsync(
        IDbConnection connection, 
        IDbTransaction transaction, 
        int eventId, 
        IEnumerable<EventSeat> seats)
    {
        var insertSql = @"
            INSERT INTO EventSeats 
            (EventId, VenueSeatId, Status)
            VALUES 
            (@EventId, @VenueSeatId, @Status);
            SELECT last_insert_rowid();";

        foreach (var seat in seats)
        {
            var dto = EventSeatMapper.ToDto(seat);
            dto.EventId = eventId;
            dto.Id = await connection.ExecuteScalarAsync<int>(insertSql, dto, transaction);
            seat.Id = dto.Id;
        }
    }

    #endregion
}
