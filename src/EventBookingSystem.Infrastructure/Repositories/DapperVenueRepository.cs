using Dapper;
using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Infrastructure.Data;
using EventBookingSystem.Infrastructure.Interfaces;
using EventBookingSystem.Infrastructure.Mapping;
using EventBookingSystem.Infrastructure.Models;

namespace EventBookingSystem.Infrastructure.Repositories;

/// <summary>
/// Dapper implementation of venue repository using optimized DTOs.
/// </summary>
public class DapperVenueRepository : IVenueRepository
{
    private readonly IDBConnectionFactory _connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DapperVenueRepository"/> class.
    /// </summary>
    /// <param name="connectionFactory">The database connection factory.</param>
    public DapperVenueRepository(IDBConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    /// <summary>
    /// Adds a new venue entity to the database, including sections and seats if present.
    /// </summary>
    /// <param name="entity">The venue entity to add.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The added <see cref="Venue"/> entity with generated IDs.</returns>
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

    /// <summary>
    /// Gets a venue by its unique identifier.
    /// </summary>
    /// <param name="id">The venue ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The <see cref="Venue"/> if found; otherwise, null.</returns>
    public async Task<Venue?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var sql = "SELECT * FROM Venues WHERE Id = @Id";
        var dto = await connection.QueryFirstOrDefaultAsync<VenueDto>(sql, new { Id = id });

        return dto != null ? VenueMapper.ToDomain(dto) : null;
    }
}