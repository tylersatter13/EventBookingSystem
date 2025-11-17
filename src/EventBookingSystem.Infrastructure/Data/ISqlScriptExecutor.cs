namespace EventBookingSystem.Infrastructure.Data;

/// <summary>
/// Defines a service for executing SQL scripts against a database.
/// </summary>
public interface ISqlScriptExecutor
{
    /// <summary>
    /// Executes a SQL script asynchronously.
    /// </summary>
    /// <param name="script">The SQL script to execute.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the number of rows affected.</returns>
    Task<int> ExecuteScriptAsync(string script, CancellationToken cancellationToken = default);


    /// <summary>
    /// Executes a SQL script from a file asynchronously.
    /// </summary>
    /// <param name="filePath">The path to the SQL script file.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the number of rows affected.</returns>
    Task<int> ExecuteScriptFromFileAsync(string filePath, CancellationToken cancellationToken = default);
}
