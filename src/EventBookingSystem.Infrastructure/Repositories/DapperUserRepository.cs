using System.Data;
using Dapper;
using EventBookingSystem.Infrastructure.Data;
using EventBookingSystem.Infrastructure.Models;
using EventBookingSystem.Infrastructure.Interfaces;
using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Infrastructure.Mapping;

namespace EventBookingSystem.Infrastructure.Repositories;

/// <summary>
/// Dapper implementation of user repository using optimized DTOs.
/// </summary>
public class DapperUserRepository : IUserRepository
{
    private readonly IDBConnectionFactory _connectionFactory;

    public DapperUserRepository(IDBConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<User> AddAsync(User entity, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var dto = UserMapper.ToDto(entity);

        var sql = @"
            INSERT INTO Users (Name, Email, PhoneNumber)
            VALUES (@Name, @Email, @PhoneNumber);
            SELECT last_insert_rowid();";

        dto.Id = await connection.ExecuteScalarAsync<int>(sql, dto);
        entity.Id = dto.Id;

        return entity;
    }

    public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var sql = "SELECT * FROM Users WHERE Id = @Id";
        var dto = await connection.QueryFirstOrDefaultAsync<UserDto>(sql, new { Id = id });

        return dto != null ? UserMapper.ToDomain(dto) : null;
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var sql = "SELECT * FROM Users WHERE Email = @Email";
        var dto = await connection.QueryFirstOrDefaultAsync<UserDto>(sql, new { Email = email });

        return dto != null ? UserMapper.ToDomain(dto) : null;
    }
}
