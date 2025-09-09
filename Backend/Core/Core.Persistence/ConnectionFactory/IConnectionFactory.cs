using System.Data;

namespace Core.Persistence.ConnectionFactory;

/// <summary>
/// Interface for database connection factory
/// </summary>
/// <typeparam name="T">The connection type</typeparam>
public interface IConnectionFactory<out T> : IDisposable where T : IDbConnection
{
    /// <summary>
    /// Gets a new database connection
    /// </summary>
    T Connection { get; }
}

/// <summary>
/// Interface for async database connection factory
/// </summary>
/// <typeparam name="T">The connection type</typeparam>
public interface IAsyncConnectionFactory<T> : IDisposable where T : IDbConnection
{
    /// <summary>
    /// Gets a new database connection asynchronously
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Database connection</returns>
    Task<T> GetConnectionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Base interface for connection factory with common operations
/// </summary>
public interface IDbConnectionFactory : IDisposable
{
    /// <summary>
    /// Gets a new database connection
    /// </summary>
    /// <returns>Database connection</returns>
    IDbConnection GetConnection();

    /// <summary>
    /// Gets a new database connection asynchronously
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Database connection</returns>
    Task<IDbConnection> GetConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the database connection
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connection is successful</returns>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the connection string
    /// </summary>
    string ConnectionString { get; }

    /// <summary>
    /// Gets the database provider name
    /// </summary>
    string ProviderName { get; }
}
