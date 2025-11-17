using System.Data;
using System.IO;
using System.Text.RegularExpressions;

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
        
        // Split script into individual statements for SQLite compatibility
        var statements = SplitSqlStatements(script);
        int totalRowsAffected = 0;

        foreach (var statement in statements)
        {
            if (string.IsNullOrWhiteSpace(statement))
                continue;

            using var command = connection.CreateCommand();
            command.CommandText = statement;
            var rowsAffected = command.ExecuteNonQuery();
            
            // Only count positive results (INSERT, UPDATE, DELETE)
            if (rowsAffected > 0)
                totalRowsAffected += rowsAffected;
        }

        return totalRowsAffected;
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

    /// <summary>
    /// Splits a SQL script into individual statements by semicolons.
    /// Handles comments and multi-line statements correctly.
    /// </summary>
    /// <param name="script">The SQL script to split.</param>
    /// <returns>An array of individual SQL statements.</returns>
    private static string[] SplitSqlStatements(string script)
    {
        // Remove SQL comments (-- and /* */)
        script = Regex.Replace(script, @"--.*?$","",RegexOptions.Multiline);
        script = Regex.Replace(script, @"/\*.*?\*/", "", RegexOptions.Singleline);

        // Split by semicolons and filter out empty statements
        return script.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();
    }
}
