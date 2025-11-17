using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Infrastructure.Data;
using EventBookingSystem.Infrastructure.Interfaces;
using EventBookingSystem.Infrastructure.Mapping;
using EventBookingSystem.Infrastructure.Models;
using Dapper;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EventBookingSystem.Infrastructure.Repositories
{
    /// <summary>
    /// Dapper implementation of General Admission event repository.
    /// Provides focused operations for GA events using GeneralAdmissionEventDto.
    /// </summary>
    public class DapperGeneralAdmissionRepository : IGeneralAdmissionEventRepository
    {
        private readonly IDBConnectionFactory _connectionFactory;

        public DapperGeneralAdmissionRepository(IDBConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public async Task<GeneralAdmissionEvent> AddAsync(GeneralAdmissionEvent entity, CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            using var transaction = connection.BeginTransaction();

            try
            {
                // Convert to focused GA DTO
                var gaDto = GeneralAdmissionEventMapper.ToDto(entity);

                // Convert to TPH EventDto for storage
                var eventDto = GeneralAdmissionEventMapper.ToEventDto(gaDto);

                // Insert into Events table using TPH pattern
                var sql = @"
                    INSERT INTO Events 
                    (VenueId, Name, StartsAt, EndsAt, EstimatedAttendance, EventType, 
                     GA_Capacity, GA_Attendees, GA_Price, GA_CapacityOverride)
                    VALUES 
                    (@VenueId, @Name, @StartsAt, @EndsAt, @EstimatedAttendance, @EventType,
                     @GA_Capacity, @GA_Attendees, @GA_Price, @GA_CapacityOverride);
                    SELECT last_insert_rowid();";

                eventDto.Id = await connection.ExecuteScalarAsync<int>(sql, eventDto, transaction);
                entity.Id = eventDto.Id;

                transaction.Commit();

              
                return entity;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<GeneralAdmissionEvent?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            // Query only GA events
            var sql = @"
                SELECT * FROM Events 
                WHERE Id = @Id AND EventType = 'GeneralAdmission'";

            var eventDto = await connection.QueryFirstOrDefaultAsync<EventDto>(sql, new { Id = id });

            if (eventDto == null)
                return null;

            // Convert from TPH EventDto to focused GA DTO
            var gaDto = GeneralAdmissionEventMapper.FromEventDto(eventDto);

            // Convert to domain entity
            var gaEvent = GeneralAdmissionEventMapper.ToDomain(gaDto);

            // Load Venue navigation property
            var venueSql = "SELECT * FROM Venues WHERE Id = @Id";
            var venueDto = await connection.QueryFirstOrDefaultAsync<VenueDto>(venueSql, new { Id = eventDto.VenueId });
            if (venueDto != null)
            {
                gaEvent.Venue = VenueMapper.ToDomain(venueDto);
            }

            return gaEvent;
        }

        public async Task<GeneralAdmissionEvent> UpdateAsync(EventBase entity, CancellationToken cancellationToken = default)
        {
            if (entity is not GeneralAdmissionEvent gaEvent)
            {
                throw new ArgumentException("Entity must be a GeneralAdmissionEvent", nameof(entity));
            }

            using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            using var transaction = connection.BeginTransaction();

            try
            {
                // Convert to focused GA DTO
                var gaDto = GeneralAdmissionEventMapper.ToDto(gaEvent);

                // Convert to TPH EventDto for storage
                var eventDto = GeneralAdmissionEventMapper.ToEventDto(gaDto);

                // Update in Events table
                var sql = @"
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
                    WHERE Id = @Id AND EventType = 'GeneralAdmission'";

                var rowsAffected = await connection.ExecuteAsync(sql, eventDto, transaction);

                if (rowsAffected == 0)
                {
                    throw new InvalidOperationException($"General Admission event with ID {gaEvent.Id} not found.");
                }

                transaction.Commit();
                return gaEvent;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
