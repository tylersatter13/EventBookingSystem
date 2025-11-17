using System.Data;
using Dapper;
using EventBookingSystem.Infrastructure.Data;
using EventBookingSystem.Infrastructure.Models;
using EventBookingSystem.Infrastructure.Interfaces;
using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Infrastructure.Mapping;

namespace EventBookingSystem.Infrastructure.Repositories;

/// <summary>
/// Dapper implementation of venue repository using optimized DTOs.
/// </summary>
public class DapperVenueRepository : IVenueRepository
{
    private readonly IDBConnectionFactory _connectionFactory;

    public DapperVenueRepository(IDBConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<Venue> AddAsync(Venue entity, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        try
        {
            var dto = VenueMapper.ToDto(entity);

            // Insert venue
            var venueSql = @"
                INSERT INTO Venues (Name, Address)
                VALUES (@Name, @Address);
                SELECT last_insert_rowid();";

            dto.Id = await connection.ExecuteScalarAsync<int>(venueSql, dto, transaction);
            entity.Id = dto.Id;

            // Insert venue sections if present
            if (entity.VenueSections != null && entity.VenueSections.Any())
            {
                foreach (var section in entity.VenueSections)
                {
                    var sectionDto = VenueSectionMapper.ToDto(section);
                    sectionDto.VenueId = entity.Id;

                    var sectionSql = @"
                        INSERT INTO VenueSections (VenueId, Name)
                        VALUES (@VenueId, @Name);
                        SELECT last_insert_rowid();";

                    sectionDto.Id = await connection.ExecuteScalarAsync<int>(sectionSql, sectionDto, transaction);
                    section.Id = sectionDto.Id;

                    // Insert venue seats for this section if present
                    if (section.VenueSeats != null && section.VenueSeats.Any())
                    {
                        foreach (var seat in section.VenueSeats)
                        {
                            var seatSql = @"
                                INSERT INTO VenueSeats (VenueSectionId, Row, SeatNumber, SeatLabel)
                                VALUES (@VenueSectionId, @Row, @SeatNumber, @SeatLabel);
                                SELECT last_insert_rowid();";

                            var seatParams = new
                            {
                                VenueSectionId = section.Id,
                                Row = seat.Row,
                                SeatNumber = seat.SeatNumber,
                                SeatLabel = seat.SeatLabel
                            };

                            seat.Id = await connection.ExecuteScalarAsync<int>(seatSql, seatParams, transaction);
                        }
                    }
                }
            }

            transaction.Commit();
            return entity;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<Venue?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var sql = "SELECT * FROM Venues WHERE Id = @Id";
        var dto = await connection.QueryFirstOrDefaultAsync<VenueDto>(sql, new { Id = id });

        return dto != null ? VenueMapper.ToDomain(dto) : null;
    }
}