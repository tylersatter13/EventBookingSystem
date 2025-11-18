using Dapper;
using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Infrastructure.Data;
using EventBookingSystem.Application.Interfaces;
using EventBookingSystem.Infrastructure.Mapping;
using EventBookingSystem.Infrastructure.Models;
using System.Data;

namespace EventBookingSystem.Infrastructure.Repositories;

/// <summary>
/// Dapper implementation of event repository supporting polymorphic event types.
/// Uses Table Per Hierarchy (TPH) pattern with discriminator column.
/// </summary>
public class DapperEventRepository : IEventRepository
{
    private readonly IDBConnectionFactory _connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DapperEventRepository"/> class.
    /// </summary>
    /// <param name="connectionFactory">The database connection factory.</param>
    public DapperEventRepository(IDBConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    #region Basic CRUD Operations

    /// <summary>
    /// Adds a new event to the database, including related data for polymorphic event types.
    /// </summary>
    /// <param name="entity">The event entity to add.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The added <see cref="EventBase"/> entity with generated IDs.</returns>
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

    /// <summary>
    /// Gets an event by its unique identifier.
    /// </summary>
    /// <param name="id">The event ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The <see cref="EventBase"/> if found; otherwise, null.</returns>
    public async Task<EventBase?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var sql = "SELECT * FROM Events WHERE Id = @Id";
        var dto = await connection.QueryFirstOrDefaultAsync<EventDto>(sql, new { Id = id });

        if (dto == null)
            return null;

        var eventBase = EventMapper.ToDomain(dto);

        // Load Venue navigation property
        var venueSql = "SELECT * FROM Venues WHERE Id = @Id";
        var venueDto = await connection.QueryFirstOrDefaultAsync<VenueDto>(venueSql, new { Id = dto.VenueId });
        if (venueDto != null)
        {
            eventBase.Venue = VenueMapper.ToDomain(venueDto);
        }

        return eventBase;
    }

    /// <summary>
    /// Gets an event by its unique identifier, including all related details (sections, seats, inventories).
    /// </summary>
    /// <param name="id">The event ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The <see cref="EventBase"/> with details if found; otherwise, null.</returns>
    public async Task<EventBase?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var sql = "SELECT * FROM Events WHERE Id = @Id";
        var dto = await connection.QueryFirstOrDefaultAsync<EventDto>(sql, new { Id = id });

        if (dto == null)
            return null;

        var eventBase = EventMapper.ToDomain(dto);

        // Load Venue navigation property
        var venueSql = "SELECT * FROM Venues WHERE Id = @Id";
        var venueDto = await connection.QueryFirstOrDefaultAsync<VenueDto>(venueSql, new { Id = dto.VenueId });
        if (venueDto != null)
        {
            eventBase.Venue = VenueMapper.ToDomain(venueDto);
        }

        // Load related data based on event type
        await LoadRelatedDataAsync(connection, eventBase);

        return eventBase;
    }

    /// <summary>
    /// Updates an existing event and its related data.
    /// </summary>
    /// <param name="entity">The event entity to update.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The updated <see cref="EventBase"/> entity.</returns>
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

    /// <summary>
    /// Deletes an event by its unique identifier.
    /// </summary>
    /// <param name="id">The event ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>True if the event was deleted; otherwise, false.</returns>
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var sql = "DELETE FROM Events WHERE Id = @Id";
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });

        return rowsAffected > 0;
    }

    #endregion

    #region Query Operations

    /// <summary>
    /// Gets all events for a specific venue.
    /// </summary>
    /// <param name="venueId">The venue ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A collection of events for the venue.</returns>
    public async Task<IEnumerable<EventBase>> GetByVenueIdAsync(int venueId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var sql = "SELECT * FROM Events WHERE VenueId = @VenueId ORDER BY StartsAt";
        var dtos = await connection.QueryAsync<EventDto>(sql, new { VenueId = venueId });

        return dtos.Select(EventMapper.ToDomain).ToList();
    }

    /// <summary>
    /// Gets events within a specified date range.
    /// </summary>
    /// <param name="startDate">The start date (inclusive).</param>
    /// <param name="endDate">The end date (inclusive).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A collection of events within the date range.</returns>
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

    /// <summary>
    /// Adds or updates section inventories for a section-based event.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="inventories">The section inventories.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
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

    /// <summary>
    /// Adds or updates event seats for a reserved seating event.
    /// </summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="seats">The event seats.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task SaveEventSeatsAsync(int eventId, IEnumerable<EventSeat> seats, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        try
        {
           
            foreach (var seat in seats)
            {
                if (seat.Id > 0)
                {
                    // Update existing seat
                    var updateSql = @"
                        UPDATE EventSeats 
                        SET Status = @Status
                        WHERE Id = @Id";
                    
                    var dto = EventSeatMapper.ToDto(seat);
                    await connection.ExecuteAsync(updateSql, dto, transaction);
                }
                else
                {
                    // Insert new seat
                    var insertSql = @"
                        INSERT INTO EventSeats 
                        (EventId, VenueSeatId, Status)
                        VALUES 
                        (@EventId, @VenueSeatId, @Status);
                        SELECT last_insert_rowid();";
                    
                    var dto = EventSeatMapper.ToDto(seat);
                    dto.EventId = eventId;
                    dto.Id = await connection.ExecuteScalarAsync<int>(insertSql, dto, transaction);
                    seat.Id = dto.Id;
                }
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

    /// <summary>
    /// Builds the SQL insert statement for the specified event type.
    /// </summary>
    /// <param name="eventType">The event type discriminator.</param>
    /// <returns>The SQL insert statement.</returns>
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

    /// <summary>
    /// Builds the SQL update statement for the specified event type.
    /// </summary>
    /// <param name="eventType">The event type discriminator.</param>
    /// <returns>The SQL update statement.</returns>
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

    /// <summary>
    /// Saves related data for the event based on its type (sections or seats).
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="transaction">The database transaction.</param>
    /// <param name="eventBase">The event entity.</param>
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

    /// <summary>
    /// Updates related data for the event based on its type (sections or seats).
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="transaction">The database transaction.</param>
    /// <param name="eventBase">The event entity.</param>
    private async Task UpdateRelatedDataAsync(IDbConnection connection, IDbTransaction transaction, EventBase eventBase)
    {
        // Same as SaveRelatedDataAsync since we replace all related data
        await SaveRelatedDataAsync(connection, transaction, eventBase);
    }

    /// <summary>
    /// Loads related data for the event based on its type (sections or seats).
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="eventBase">The event entity.</param>
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

    /// <summary>
    /// Loads section inventories for a section-based event.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="sbEvent">The section-based event.</param>
    private async Task LoadSectionInventoriesAsync(IDbConnection connection, SectionBasedEvent sbEvent)
    {
        var sql = "SELECT * FROM EventSectionInventories WHERE EventId = @EventId";
        var dtos = await connection.QueryAsync<EventSectionInventoryDto>(sql, new { EventId = sbEvent.Id });

        sbEvent.SectionInventories = dtos
            .Select(EventSectionInventoryMapper.ToDomain)
            .ToList();
    }

    /// <summary>
    /// Loads event seats for a reserved seating event.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="rsEvent">The reserved seating event.</param>
    private async Task LoadEventSeatsAsync(IDbConnection connection, ReservedSeatingEvent rsEvent)
    {
        var sql = "SELECT * FROM EventSeats WHERE EventId = @EventId";
        var dtos = await connection.QueryAsync<EventSeatDto>(sql, new { EventId = rsEvent.Id });

        rsEvent.Seats = dtos
            .Select(EventSeatMapper.ToDomain)
            .ToList();
    }

    /// <summary>
    /// Saves section inventories for a section-based event, handling both inserts and updates.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="transaction">The database transaction.</param>
    /// <param name="eventId">The event ID.</param>
    /// <param name="inventories">The section inventories.</param>
    private async Task SaveSectionInventoriesInternalAsync(
        IDbConnection connection, 
        IDbTransaction transaction, 
        int eventId, 
        IEnumerable<EventSectionInventory> inventories)
    {
        
        foreach (var inventory in inventories)
        {
            if (inventory.Id > 0)
            {
                // Update existing inventory
                var updateSql = @"
                    UPDATE EventSectionInventories 
                    SET Capacity = @Capacity, 
                        Booked = @Booked, 
                        Price = @Price, 
                        AllocationMode = @AllocationMode
                    WHERE Id = @Id";
                
                var dto = EventSectionInventoryMapper.ToDto(inventory);
                await connection.ExecuteAsync(updateSql, dto, transaction);
            }
            else
            {
                // Insert new inventory
                var insertSql = @"
                    INSERT INTO EventSectionInventories 
                    (EventId, VenueSectionId, Capacity, Booked, Price, AllocationMode)
                    VALUES 
                    (@EventId, @VenueSectionId, @Capacity, @Booked, @Price, @AllocationMode);
                    SELECT last_insert_rowid();";
                
                var dto = EventSectionInventoryMapper.ToDto(inventory);
                dto.EventId = eventId;
                dto.Id = await connection.ExecuteScalarAsync<int>(insertSql, dto, transaction);
                inventory.Id = dto.Id;
            }
        }
    }

    /// <summary>
    /// Saves event seats for a reserved seating event, handling both inserts and updates.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="transaction">The database transaction.</param>
    /// <param name="eventId">The event ID.</param>
    /// <param name="seats">The event seats.</param>
    private async Task SaveEventSeatsInternalAsync(
        IDbConnection connection, 
        IDbTransaction transaction, 
        int eventId, 
        IEnumerable<EventSeat> seats)
    {
        foreach (var seat in seats)
        {
            if (seat.Id > 0)
            {
                // Update existing seat
                var updateSql = @"
                    UPDATE EventSeats 
                    SET Status = @Status
                    WHERE Id = @Id";
                
                var dto = EventSeatMapper.ToDto(seat);
                await connection.ExecuteAsync(updateSql, dto, transaction);
            }
            else
            {
                // Insert new seat
                var insertSql = @"
                    INSERT INTO EventSeats 
                    (EventId, VenueSeatId, Status)
                    VALUES 
                    (@EventId, @VenueSeatId, @Status);
                    SELECT last_insert_rowid();";
                
                var dto = EventSeatMapper.ToDto(seat);
                dto.EventId = eventId;
                dto.Id = await connection.ExecuteScalarAsync<int>(insertSql, dto, transaction);
                seat.Id = dto.Id;
            }
        }
    }

    #endregion
}
