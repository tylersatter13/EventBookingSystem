using System.Data;
using System.IO;

namespace EventBookingSystem.Infrastructure.Data;

/// <summary>
/// Executes SQL scripts against a database using a connection factory.
/// Follows Single Responsibility Principle by focusing only on script execution.
/// </summary>
public class SqlScriptExecutor : ISqlScriptExecutor
{
    private readonly IDBConnectionFactory _connectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlScriptExecutor"/> class.
    /// </summary>
    /// <param name="connectionFactory">The connection factory to create database connections.</param>
    /// <exception cref="ArgumentNullException">Thrown when connectionFactory is null.</exception>
    public SqlScriptExecutor(IDBConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    /// <inheritdoc/>
    public async Task<int> ExecuteScriptAsync(string script, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(script))
        {
            throw new ArgumentException("Script cannot be null or empty.", nameof(script));
        }

        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText = script;
        return command.ExecuteNonQuery();
    }


    /// <inheritdoc/>
    public async Task<int> ExecuteScriptFromFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"SQL script file not found: {filePath}", filePath);
        }

        var script = await File.ReadAllTextAsync(filePath, cancellationToken);
        return await ExecuteScriptAsync(script, cancellationToken);
    }
}
