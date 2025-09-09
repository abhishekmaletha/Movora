using Dapper;
using System.Data;

namespace Core.Persistence.Helpers;

/// <summary>
/// Interface for database helper operations
/// </summary>
public interface IDatabaseHelper
{
    /// <summary>
    /// Executes a query and returns a single result
    /// </summary>
    /// <typeparam name="T">Result type</typeparam>
    /// <param name="sql">SQL query</param>
    /// <param name="parameters">Query parameters</param>
    /// <param name="commandType">Command type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Single result</returns>
    Task<T?> QuerySingleOrDefaultAsync<T>(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a query and returns multiple results
    /// </summary>
    /// <typeparam name="T">Result type</typeparam>
    /// <param name="sql">SQL query</param>
    /// <param name="parameters">Query parameters</param>
    /// <param name="commandType">Command type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Multiple results</returns>
    Task<IEnumerable<T>> QueryAsync<T>(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a command and returns the number of affected rows
    /// </summary>
    /// <param name="sql">SQL command</param>
    /// <param name="parameters">Command parameters</param>
    /// <param name="commandType">Command type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of affected rows</returns>
    Task<int> ExecuteAsync(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a command and returns a scalar result
    /// </summary>
    /// <typeparam name="T">Result type</typeparam>
    /// <param name="sql">SQL command</param>
    /// <param name="parameters">Command parameters</param>
    /// <param name="commandType">Command type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Scalar result</returns>
    Task<T?> ExecuteScalarAsync<T>(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes multiple queries and returns multiple result sets
    /// </summary>
    /// <param name="sql">SQL queries</param>
    /// <param name="parameters">Query parameters</param>
    /// <param name="commandType">Command type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Multiple result sets</returns>
    Task<SqlMapper.GridReader> QueryMultipleAsync(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes operations within a transaction
    /// </summary>
    /// <typeparam name="T">Result type</typeparam>
    /// <param name="operation">Operation to execute</param>
    /// <param name="isolationLevel">Transaction isolation level</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<T> ExecuteInTransactionAsync<T>(
        Func<IDbTransaction, CancellationToken, Task<T>> operation,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes operations within a transaction
    /// </summary>
    /// <param name="operation">Operation to execute</param>
    /// <param name="isolationLevel">Transaction isolation level</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ExecuteInTransactionAsync(
        Func<IDbTransaction, CancellationToken, Task> operation,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for PostgreSQL-specific database operations
/// </summary>
public interface IPostgreSqlHelper : IDatabaseHelper
{
    /// <summary>
    /// Executes a PostgreSQL function
    /// </summary>
    /// <typeparam name="T">Result type</typeparam>
    /// <param name="functionName">Function name</param>
    /// <param name="parameters">Function parameters</param>
    /// <param name="schemaName">Schema name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Function result</returns>
    Task<T?> ExecuteFunctionAsync<T>(
        string functionName,
        object? parameters = null,
        string schemaName = "public",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a PostgreSQL function that returns multiple results
    /// </summary>
    /// <typeparam name="T">Result type</typeparam>
    /// <param name="functionName">Function name</param>
    /// <param name="parameters">Function parameters</param>
    /// <param name="schemaName">Schema name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Function results</returns>
    Task<IEnumerable<T>> ExecuteFunctionMultipleAsync<T>(
        string functionName,
        object? parameters = null,
        string schemaName = "public",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs bulk insert using PostgreSQL COPY command
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="tableName">Table name</param>
    /// <param name="data">Data to insert</param>
    /// <param name="schemaName">Schema name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of rows inserted</returns>
    Task<long> BulkInsertAsync<T>(
        string tableName,
        IEnumerable<T> data,
        string schemaName = "public",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a composite type parameter for PostgreSQL
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="data">Data collection</param>
    /// <param name="typeName">Composite type name</param>
    /// <returns>Composite type parameter</returns>
    object CreateCompositeTypeParameter<T>(IEnumerable<T> data, string typeName);

    /// <summary>
    /// Registers a composite type mapping
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="typeName">Composite type name</param>
    void RegisterCompositeType<T>(string typeName);

    /// <summary>
    /// Creates a PostgreSQL array parameter
    /// </summary>
    /// <typeparam name="T">Array element type</typeparam>
    /// <param name="data">Array data</param>
    /// <returns>Array parameter</returns>
    object CreateArrayParameter<T>(IEnumerable<T> data);
}
