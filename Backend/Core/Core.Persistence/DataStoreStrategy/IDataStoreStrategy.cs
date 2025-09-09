using System.Data;

namespace Core.Persistence.DataStoreStrategy;

/// <summary>
/// Interface for data store strategy pattern
/// </summary>
public interface IDataStoreStrategy
{
    /// <summary>
    /// Database provider name
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Executes a query and returns a single result
    /// </summary>
    /// <typeparam name="T">Result type</typeparam>
    /// <param name="connection">Database connection</param>
    /// <param name="sql">SQL query</param>
    /// <param name="parameters">Query parameters</param>
    /// <param name="transaction">Database transaction</param>
    /// <param name="commandType">Command type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Single result</returns>
    Task<T?> QuerySingleOrDefaultAsync<T>(
        IDbConnection connection,
        string sql,
        object? parameters = null,
        IDbTransaction? transaction = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a query and returns multiple results
    /// </summary>
    /// <typeparam name="T">Result type</typeparam>
    /// <param name="connection">Database connection</param>
    /// <param name="sql">SQL query</param>
    /// <param name="parameters">Query parameters</param>
    /// <param name="transaction">Database transaction</param>
    /// <param name="commandType">Command type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Multiple results</returns>
    Task<IEnumerable<T>> QueryAsync<T>(
        IDbConnection connection,
        string sql,
        object? parameters = null,
        IDbTransaction? transaction = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a command and returns the number of affected rows
    /// </summary>
    /// <param name="connection">Database connection</param>
    /// <param name="sql">SQL command</param>
    /// <param name="parameters">Command parameters</param>
    /// <param name="transaction">Database transaction</param>
    /// <param name="commandType">Command type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of affected rows</returns>
    Task<int> ExecuteAsync(
        IDbConnection connection,
        string sql,
        object? parameters = null,
        IDbTransaction? transaction = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs bulk insert operation
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="connection">Database connection</param>
    /// <param name="tableName">Table name</param>
    /// <param name="data">Data to insert</param>
    /// <param name="transaction">Database transaction</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of rows inserted</returns>
    Task<long> BulkInsertAsync<T>(
        IDbConnection connection,
        string tableName,
        IEnumerable<T> data,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a parameter for bulk operations
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="data">Data collection</param>
    /// <param name="typeName">Type name</param>
    /// <returns>Bulk parameter</returns>
    object CreateBulkParameter<T>(IEnumerable<T> data, string typeName);

    /// <summary>
    /// Gets the parameter prefix for the database provider
    /// </summary>
    string ParameterPrefix { get; }

    /// <summary>
    /// Gets the quote character for identifiers
    /// </summary>
    string QuoteCharacter { get; }

    /// <summary>
    /// Escapes an identifier (table name, column name, etc.)
    /// </summary>
    /// <param name="identifier">Identifier to escape</param>
    /// <returns>Escaped identifier</returns>
    string EscapeIdentifier(string identifier);

    /// <summary>
    /// Builds a qualified table name with schema
    /// </summary>
    /// <param name="tableName">Table name</param>
    /// <param name="schemaName">Schema name</param>
    /// <returns>Qualified table name</returns>
    string BuildQualifiedTableName(string tableName, string? schemaName = null);
}

/// <summary>
/// Interface for advanced data store strategy operations
/// </summary>
public interface IAdvancedDataStoreStrategy : IDataStoreStrategy
{
    /// <summary>
    /// Executes a stored procedure or function
    /// </summary>
    /// <typeparam name="T">Result type</typeparam>
    /// <param name="connection">Database connection</param>
    /// <param name="procedureName">Procedure name</param>
    /// <param name="parameters">Procedure parameters</param>
    /// <param name="transaction">Database transaction</param>
    /// <param name="schemaName">Schema name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Procedure result</returns>
    Task<T?> ExecuteProcedureAsync<T>(
        IDbConnection connection,
        string procedureName,
        object? parameters = null,
        IDbTransaction? transaction = null,
        string? schemaName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a stored procedure or function that returns multiple results
    /// </summary>
    /// <typeparam name="T">Result type</typeparam>
    /// <param name="connection">Database connection</param>
    /// <param name="procedureName">Procedure name</param>
    /// <param name="parameters">Procedure parameters</param>
    /// <param name="transaction">Database transaction</param>
    /// <param name="schemaName">Schema name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Procedure results</returns>
    Task<IEnumerable<T>> ExecuteProcedureMultipleAsync<T>(
        IDbConnection connection,
        string procedureName,
        object? parameters = null,
        IDbTransaction? transaction = null,
        string? schemaName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs bulk update operation
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="connection">Database connection</param>
    /// <param name="tableName">Table name</param>
    /// <param name="data">Data to update</param>
    /// <param name="keyColumns">Key columns for matching</param>
    /// <param name="transaction">Database transaction</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of rows updated</returns>
    Task<long> BulkUpdateAsync<T>(
        IDbConnection connection,
        string tableName,
        IEnumerable<T> data,
        string[] keyColumns,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs bulk upsert (insert or update) operation
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="connection">Database connection</param>
    /// <param name="tableName">Table name</param>
    /// <param name="data">Data to upsert</param>
    /// <param name="keyColumns">Key columns for matching</param>
    /// <param name="transaction">Database transaction</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of rows affected</returns>
    Task<long> BulkUpsertAsync<T>(
        IDbConnection connection,
        string tableName,
        IEnumerable<T> data,
        string[] keyColumns,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the database provider supports a specific feature
    /// </summary>
    /// <param name="feature">Feature to check</param>
    /// <returns>True if supported</returns>
    bool SupportsFeature(DatabaseFeature feature);
}

/// <summary>
/// Database features enumeration
/// </summary>
public enum DatabaseFeature
{
    /// <summary>
    /// Array data types
    /// </summary>
    Arrays,

    /// <summary>
    /// JSON data types
    /// </summary>
    Json,

    /// <summary>
    /// User-defined types
    /// </summary>
    UserDefinedTypes,

    /// <summary>
    /// Table-valued parameters
    /// </summary>
    TableValuedParameters,

    /// <summary>
    /// Composite types
    /// </summary>
    CompositeTypes,

    /// <summary>
    /// Bulk copy operations
    /// </summary>
    BulkCopy,

    /// <summary>
    /// Common table expressions (CTEs)
    /// </summary>
    CommonTableExpressions,

    /// <summary>
    /// Window functions
    /// </summary>
    WindowFunctions,

    /// <summary>
    /// Recursive queries
    /// </summary>
    RecursiveQueries,

    /// <summary>
    /// Full-text search
    /// </summary>
    FullTextSearch
}
