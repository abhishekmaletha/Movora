using Core.Persistence.ConnectionFactory;
using Core.Persistence.Extensions;
using Core.Persistence.Helpers;
using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using System.Data;
using System.Text;

namespace Core.Persistence.Helpers;

/// <summary>
/// PostgreSQL database helper implementation
/// </summary>
public class PostgreSqlHelper : IPostgreSqlHelper
{
    private readonly IConnectionFactory<IDbConnection> _connectionFactory;
    private readonly ILogger<PostgreSqlHelper> _logger;
    private readonly PostgreSqlRetryHelper _retryHelper;

    /// <summary>
    /// Initializes a new instance of PostgreSqlHelper
    /// </summary>
    public PostgreSqlHelper(
        IConnectionFactory<IDbConnection> connectionFactory,
        ILogger<PostgreSqlHelper> logger,
        PostgreSqlRetryHelper retryHelper)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _retryHelper = retryHelper ?? throw new ArgumentNullException(nameof(retryHelper));
    }

    /// <inheritdoc />
    public async Task<T?> QuerySingleOrDefaultAsync<T>(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default)
    {
        return await _retryHelper.ExecuteWithRetryAsync(async () =>
        {
            using var connection = _connectionFactory.Connection;
            _logger.LogDebug("Executing query single: {Sql}", sql);
            
            return await connection.QuerySingleOrDefaultAsync<T>(sql, parameters, commandType: commandType);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<T>> QueryAsync<T>(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default)
    {
        return await _retryHelper.ExecuteWithRetryAsync(async () =>
        {
            using var connection = _connectionFactory.Connection;
            _logger.LogDebug("Executing query: {Sql}", sql);
            
            return await connection.QueryAsync<T>(sql, parameters, commandType: commandType);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> ExecuteAsync(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default)
    {
        return await _retryHelper.ExecuteWithRetryAsync(async () =>
        {
            using var connection = _connectionFactory.Connection;
            _logger.LogDebug("Executing command: {Sql}", sql);
            
            return await connection.ExecuteAsync(sql, parameters, commandType: commandType);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<T?> ExecuteScalarAsync<T>(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default)
    {
        return await _retryHelper.ExecuteWithRetryAsync(async () =>
        {
            using var connection = _connectionFactory.Connection;
            _logger.LogDebug("Executing scalar: {Sql}", sql);
            
            return await connection.ExecuteScalarAsync<T>(sql, parameters, commandType: commandType);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SqlMapper.GridReader> QueryMultipleAsync(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default)
    {
        return await _retryHelper.ExecuteWithRetryAsync(async () =>
        {
            using var connection = _connectionFactory.Connection;
            _logger.LogDebug("Executing query multiple: {Sql}", sql);
            
            return await connection.QueryMultipleAsync(sql, parameters, commandType: commandType);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<IDbTransaction, CancellationToken, Task<T>> operation,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        return await _retryHelper.ExecuteWithRetryAsync(async () =>
        {
            using var connection = _connectionFactory.Connection;
            if (connection.State != ConnectionState.Open)
            {
                if (connection is NpgsqlConnection npgsqlConn)
                {
                    await npgsqlConn.OpenAsync(cancellationToken);
                }
                else
                {
                    connection.Open();
                }
            }
            
            using var transaction = connection.BeginTransaction(isolationLevel);
            try
            {
                _logger.LogDebug("Starting transaction with isolation level: {IsolationLevel}", isolationLevel);
                
                var result = await operation(transaction, cancellationToken);
                transaction.Commit();
                
                _logger.LogDebug("Transaction committed successfully");
                return result;
            }
            catch
            {
                transaction.Rollback();
                _logger.LogWarning("Transaction rolled back due to error");
                throw;
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ExecuteInTransactionAsync(
        Func<IDbTransaction, CancellationToken, Task> operation,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        await ExecuteInTransactionAsync(async (transaction, ct) =>
        {
            await operation(transaction, ct);
            return true;
        }, isolationLevel, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<T?> ExecuteFunctionAsync<T>(
        string functionName,
        object? parameters = null,
        string schemaName = "public",
        CancellationToken cancellationToken = default)
    {
        var sql = $"SELECT * FROM {schemaName}.{functionName}({BuildParameterPlaceholders(parameters)})";
        return await QuerySingleOrDefaultAsync<T>(sql, parameters, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<T>> ExecuteFunctionMultipleAsync<T>(
        string functionName,
        object? parameters = null,
        string schemaName = "public",
        CancellationToken cancellationToken = default)
    {
        var sql = $"SELECT * FROM {schemaName}.{functionName}({BuildParameterPlaceholders(parameters)})";
        return await QueryAsync<T>(sql, parameters, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> BulkInsertAsync<T>(
        string tableName,
        IEnumerable<T> data,
        string schemaName = "public",
        CancellationToken cancellationToken = default)
    {
        return await _retryHelper.ExecuteWithRetryAsync(async () =>
        {
            using var connection = (NpgsqlConnection)_connectionFactory.Connection;
            await connection.OpenAsync(cancellationToken);

            var fullTableName = $"{schemaName}.{tableName}";
            var properties = typeof(T).GetProperties();
            var columnNames = properties.Select(p => p.GetPostgreSqlColumnName()).ToArray();
            
            _logger.LogDebug("Starting bulk insert to {TableName} with {Count} records", fullTableName, data.Count());

            using var writer = connection.BeginBinaryImport($"COPY {fullTableName} ({string.Join(", ", columnNames)}) FROM STDIN (FORMAT BINARY)");
            
            long insertedCount = 0;
            foreach (var item in data)
            {
                await writer.StartRowAsync(cancellationToken);
                
                foreach (var property in properties)
                {
                    var value = property.GetValue(item);
                    var pgType = property.GetPostgreSqlType();
                    
                    if (value == null)
                    {
                        await writer.WriteNullAsync(cancellationToken);
                    }
                    else
                    {
                        await writer.WriteAsync(value, pgType, cancellationToken);
                    }
                }
                
                insertedCount++;
            }

            await writer.CompleteAsync(cancellationToken);
            
            _logger.LogDebug("Bulk insert completed. Inserted {Count} records", insertedCount);
            return insertedCount;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public object CreateCompositeTypeParameter<T>(IEnumerable<T> data, string typeName)
    {
        var dataTable = data.CreateCompositeTable();
        return new { Data = dataTable, TypeName = typeName };
    }

    /// <inheritdoc />
    public void RegisterCompositeType<T>(string typeName)
    {
        // Register the composite type mapping with Npgsql
        // This would typically be done at startup
        _logger.LogDebug("Registering composite type: {TypeName} for {Type}", typeName, typeof(T).Name);
    }

    /// <inheritdoc />
    public object CreateArrayParameter<T>(IEnumerable<T> data)
    {
        return data.ToArray();
    }

    private string BuildParameterPlaceholders(object? parameters)
    {
        if (parameters == null)
            return string.Empty;

        var properties = parameters.GetType().GetProperties();
        return string.Join(", ", properties.Select((p, i) => $"@{p.Name}"));
    }
}

/// <summary>
/// PostgreSQL retry helper for handling transient errors
/// </summary>
public class PostgreSqlRetryHelper
{
    private readonly ILogger<PostgreSqlRetryHelper> _logger;
    private readonly int _maxRetryAttempts;
    private readonly TimeSpan _baseDelay;

    /// <summary>
    /// Initializes a new instance of PostgreSqlRetryHelper
    /// </summary>
    public PostgreSqlRetryHelper(ILogger<PostgreSqlRetryHelper> logger, int maxRetryAttempts = 3, TimeSpan? baseDelay = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _maxRetryAttempts = maxRetryAttempts;
        _baseDelay = baseDelay ?? TimeSpan.FromSeconds(1);
    }

    /// <summary>
    /// Executes an operation with retry logic
    /// </summary>
    public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        var attempt = 0;
        while (true)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (ShouldRetry(ex, attempt))
            {
                attempt++;
                var delay = TimeSpan.FromMilliseconds(_baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                
                _logger.LogWarning(ex, "Database operation failed, attempt {Attempt}/{MaxAttempts}. Retrying in {Delay}ms", 
                    attempt, _maxRetryAttempts, delay.TotalMilliseconds);
                
                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    private bool ShouldRetry(Exception exception, int attempt)
    {
        if (attempt >= _maxRetryAttempts)
            return false;

        return exception switch
        {
            PostgresException pgEx => pgEx.IsRetriableError(),
            NpgsqlException npgsqlEx => IsRetriableNpgsqlException(npgsqlEx),
            TimeoutException => true,
            _ => false
        };
    }

    private static bool IsRetriableNpgsqlException(NpgsqlException exception)
    {
        return exception.Message.Contains("timeout") ||
               exception.Message.Contains("connection") ||
               exception.Message.Contains("network");
    }
}
