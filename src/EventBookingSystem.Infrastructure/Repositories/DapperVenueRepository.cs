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

        var dto = VenueMapper.ToDto(entity);

        var sql = @"
            INSERT INTO Venues (Name, Address)
            VALUES (@Name, @Address);
            SELECT last_insert_rowid();";

        dto.Id = await connection.ExecuteScalarAsync<int>(sql, dto);
        entity.Id = dto.Id;

        return entity;
    }

    public async Task<Venue?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var sql = "SELECT * FROM Venues WHERE Id = @Id";
        var dto = await connection.QueryFirstOrDefaultAsync<VenueDto>(sql, new { Id = id });

        return dto != null ? VenueMapper.ToDomain(dto) : null;
    }
}