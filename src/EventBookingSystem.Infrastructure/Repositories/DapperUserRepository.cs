using Dapper;
using EventBookingSystem.Domain.Entities;
using EventBookingSystem.Infrastructure.Data;
using EventBookingSystem.Infrastructure.Interfaces;
using EventBookingSystem.Infrastructure.Mapping;
using EventBookingSystem.Infrastructure.Models;

namespace EventBookingSystem.Infrastructure.Repositories;

/// <summary>
/// Dapper implementation of user repository using optimized DTOs.
/// </summary>
public class DapperUserRepository : IUserRepository
{
    private readonly IDBConnectionFactory _connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DapperUserRepository"/> class.
    /// </summary>
    /// <param name="connectionFactory">The database connection factory.</param>
    public DapperUserRepository(IDBConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    /// <summary>
    /// Adds a new user entity to the database.
    /// </summary>
    /// <param name="entity">The user entity to add.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The added <see cref="User"/> entity with generated ID.</returns>
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

    /// <summary>
    /// Gets a user by their unique identifier.
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The <see cref="User"/> if found; otherwise, null.</returns>
    public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var sql = "SELECT * FROM Users WHERE Id = @Id";
        var dto = await connection.QueryFirstOrDefaultAsync<UserDto>(sql, new { Id = id });

        return dto != null ? UserMapper.ToDomain(dto) : null;
    }

    /// <summary>
    /// Gets a user by their email address.
    /// </summary>
    /// <param name="email">The email address to search for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The <see cref="User"/> if found; otherwise, null.</returns>
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var sql = "SELECT * FROM Users WHERE Email = @Email";
        var dto = await connection.QueryFirstOrDefaultAsync<UserDto>(sql, new { Email = email });

        return dto != null ? UserMapper.ToDomain(dto) : null;
    }
}
