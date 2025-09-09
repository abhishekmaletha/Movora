using Npgsql;
using System.Data;

namespace Core.Persistence.ConnectionFactory;

/// <summary>
/// PostgreSQL connection factory implementation
/// </summary>
public class PostgreSqlConnectionFactory : IConnectionFactory<IDbConnection>, IDbConnectionFactory
{
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of PostgreSqlConnectionFactory
    /// </summary>
    /// <param name="connectionString">PostgreSQL connection string</param>
    public PostgreSqlConnectionFactory(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    /// <inheritdoc />
    public IDbConnection Connection => new NpgsqlConnection(_connectionString);

    /// <inheritdoc />
    public string ConnectionString => _connectionString;

    /// <inheritdoc />
    public string ProviderName => "PostgreSQL";

    /// <inheritdoc />
    public IDbConnection GetConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }

    /// <inheritdoc />
    public async Task<IDbConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(_connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            connection?.Dispose();
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await GetConnectionAsync(cancellationToken);
            return connection.State == ConnectionState.Open;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // No cleanup needed for connection factory
        // Individual connections are disposed by their consumers
    }
}

/// <summary>
/// Typed PostgreSQL connection factory
/// </summary>
public class PostgreSqlConnectionFactory<T> : IConnectionFactory<T> where T : IDbConnection
{
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of PostgreSqlConnectionFactory
    /// </summary>
    /// <param name="connectionString">PostgreSQL connection string</param>
    public PostgreSqlConnectionFactory(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    /// <inheritdoc />
    public T Connection
    {
        get
        {
            if (typeof(T) == typeof(IDbConnection) || typeof(T) == typeof(NpgsqlConnection))
            {
                return (T)(IDbConnection)new NpgsqlConnection(_connectionString);
            }
            
            throw new NotSupportedException($"Connection type {typeof(T).Name} is not supported by PostgreSQL connection factory");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // No cleanup needed for connection factory
    }
}
