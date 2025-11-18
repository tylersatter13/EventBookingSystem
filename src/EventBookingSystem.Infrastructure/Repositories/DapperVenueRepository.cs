using Dapper;
using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Infrastructure.Data;
using EventBookingSystem.Application.Interfaces;
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

    /// <summary>
    /// Gets a venue by its ID with all sections and seats loaded.
    /// </summary>
    /// <param name="id">The venue ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The <see cref="Venue"/> with sections if found; otherwise, null.</returns>
    public async Task<Venue?> GetByIdWithSectionsAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        // Load venue
        var venueSql = "SELECT * FROM Venues WHERE Id = @Id";
        var venueDto = await connection.QueryFirstOrDefaultAsync<VenueDto>(venueSql, new { Id = id });

        if (venueDto == null)
            return null;

        var venue = VenueMapper.ToDomain(venueDto);

        // Load sections
        var sectionsSql = "SELECT * FROM VenueSections WHERE VenueId = @VenueId";
        var sectionDtos = await connection.QueryAsync<VenueSectionDto>(sectionsSql, new { VenueId = id });

        foreach (var sectionDto in sectionDtos)
        {
            var section = VenueSectionMapper.ToDomain(sectionDto);
            section.Venue = venue;

            // Load seats for this section
            var seatsSql = "SELECT * FROM VenueSeats WHERE VenueSectionId = @SectionId";
            var seatDtos = await connection.QueryAsync<VenueSeatDto>(seatsSql, new { SectionId = section.Id });

            foreach (var seatDto in seatDtos)
            {
                var seat = new VenueSeat
                {
                    Id = seatDto.Id,
                    Row = seatDto.Row,
                    SeatNumber = seatDto.SeatNumber,
                    SeatLabel = seatDto.SeatLabel,
                    Section = section
                };
                section.VenueSeats.Add(seat);
            }

            venue.VenueSections.Add(section);
        }

        return venue;
    }

    /// <summary>
    /// Gets all venues in the system.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A collection of all venues.</returns>
    public async Task<IEnumerable<Venue>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var sql = "SELECT * FROM Venues ORDER BY Name";
        var dtos = await connection.QueryAsync<VenueDto>(sql);

        return dtos.Select(VenueMapper.ToDomain).ToList();
    }
}