using Core.Persistence.Extensions;
using Dapper;
using Npgsql;
using System.Data;
using System.Text;

namespace Core.Persistence.DataStoreStrategy;

/// <summary>
/// PostgreSQL data store strategy implementation
/// </summary>
public class PostgreSqlDataStoreStrategy : IAdvancedDataStoreStrategy
{
    /// <inheritdoc />
    public string ProviderName => "PostgreSQL";

    /// <inheritdoc />
    public string ParameterPrefix => "@";

    /// <inheritdoc />
    public string QuoteCharacter => "\"";

    /// <inheritdoc />
    public async Task<T?> QuerySingleOrDefaultAsync<T>(
        IDbConnection connection,
        string sql,
        object? parameters = null,
        IDbTransaction? transaction = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default)
    {
        return await connection.QuerySingleOrDefaultAsync<T>(sql, parameters, transaction, commandType: commandType);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<T>> QueryAsync<T>(
        IDbConnection connection,
        string sql,
        object? parameters = null,
        IDbTransaction? transaction = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default)
    {
        return await connection.QueryAsync<T>(sql, parameters, transaction, commandType: commandType);
    }

    /// <inheritdoc />
    public async Task<int> ExecuteAsync(
        IDbConnection connection,
        string sql,
        object? parameters = null,
        IDbTransaction? transaction = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default)
    {
        return await connection.ExecuteAsync(sql, parameters, transaction, commandType: commandType);
    }

    /// <inheritdoc />
    public async Task<long> BulkInsertAsync<T>(
        IDbConnection connection,
        string tableName,
        IEnumerable<T> data,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        if (connection is not NpgsqlConnection npgsqlConnection)
            throw new ArgumentException("Connection must be NpgsqlConnection for PostgreSQL bulk operations", nameof(connection));

        var dataList = data.ToList();
        if (!dataList.Any())
            return 0;

        var properties = typeof(T).GetProperties();
        var columnNames = properties.Select(p => p.GetPostgreSqlColumnName()).ToArray();
        var copyCommand = $"COPY {EscapeIdentifier(tableName)} ({string.Join(", ", columnNames.Select(EscapeIdentifier))}) FROM STDIN (FORMAT BINARY)";

        using var writer = npgsqlConnection.BeginBinaryImport(copyCommand);
        
        long insertedCount = 0;
        foreach (var item in dataList)
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
        return insertedCount;
    }

    /// <inheritdoc />
    public object CreateBulkParameter<T>(IEnumerable<T> data, string typeName)
    {
        var dataTable = data.CreateCompositeTable();
        return new { Data = dataTable, TypeName = typeName };
    }

    /// <inheritdoc />
    public string EscapeIdentifier(string identifier)
    {
        if (string.IsNullOrEmpty(identifier))
            return identifier;

        return $"{QuoteCharacter}{identifier.Replace(QuoteCharacter, QuoteCharacter + QuoteCharacter)}{QuoteCharacter}";
    }

    /// <inheritdoc />
    public string BuildQualifiedTableName(string tableName, string? schemaName = null)
    {
        if (string.IsNullOrEmpty(schemaName))
            schemaName = "public";

        return $"{EscapeIdentifier(schemaName)}.{EscapeIdentifier(tableName)}";
    }

    /// <inheritdoc />
    public async Task<T?> ExecuteProcedureAsync<T>(
        IDbConnection connection,
        string procedureName,
        object? parameters = null,
        IDbTransaction? transaction = null,
        string? schemaName = null,
        CancellationToken cancellationToken = default)
    {
        var qualifiedName = BuildQualifiedTableName(procedureName, schemaName);
        var sql = $"SELECT * FROM {qualifiedName}({BuildParameterPlaceholders(parameters)})";
        
        return await QuerySingleOrDefaultAsync<T>(connection, sql, parameters, transaction, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<T>> ExecuteProcedureMultipleAsync<T>(
        IDbConnection connection,
        string procedureName,
        object? parameters = null,
        IDbTransaction? transaction = null,
        string? schemaName = null,
        CancellationToken cancellationToken = default)
    {
        var qualifiedName = BuildQualifiedTableName(procedureName, schemaName);
        var sql = $"SELECT * FROM {qualifiedName}({BuildParameterPlaceholders(parameters)})";
        
        return await QueryAsync<T>(connection, sql, parameters, transaction, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> BulkUpdateAsync<T>(
        IDbConnection connection,
        string tableName,
        IEnumerable<T> data,
        string[] keyColumns,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        var dataList = data.ToList();
        if (!dataList.Any())
            return 0;

        var properties = typeof(T).GetProperties();
        var updateColumns = properties.Where(p => !keyColumns.Contains(p.Name)).Select(p => p.Name).ToArray();
        
        var tempTableName = $"temp_{tableName}_{Guid.NewGuid():N}";
        
        try
        {
            // Create temporary table
            var createTempTableSql = BuildCreateTempTableSql<T>(tempTableName);
            await ExecuteAsync(connection, createTempTableSql, transaction: transaction, cancellationToken: cancellationToken);

            // Bulk insert into temp table
            await BulkInsertAsync(connection, tempTableName, dataList, transaction, cancellationToken);

            // Update from temp table
            var updateSql = BuildUpdateFromTempTableSql(tableName, tempTableName, keyColumns, updateColumns);
            var updatedCount = await ExecuteAsync(connection, updateSql, transaction: transaction, cancellationToken: cancellationToken);

            return updatedCount;
        }
        finally
        {
            // Drop temporary table
            await ExecuteAsync(connection, $"DROP TABLE IF EXISTS {EscapeIdentifier(tempTableName)}", transaction: transaction, cancellationToken: cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<long> BulkUpsertAsync<T>(
        IDbConnection connection,
        string tableName,
        IEnumerable<T> data,
        string[] keyColumns,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        var dataList = data.ToList();
        if (!dataList.Any())
            return 0;

        var properties = typeof(T).GetProperties();
        var allColumns = properties.Select(p => p.Name).ToArray();
        var updateColumns = properties.Where(p => !keyColumns.Contains(p.Name)).Select(p => p.Name).ToArray();
        
        var tempTableName = $"temp_{tableName}_{Guid.NewGuid():N}";
        
        try
        {
            // Create temporary table
            var createTempTableSql = BuildCreateTempTableSql<T>(tempTableName);
            await ExecuteAsync(connection, createTempTableSql, transaction: transaction, cancellationToken: cancellationToken);

            // Bulk insert into temp table
            await BulkInsertAsync(connection, tempTableName, dataList, transaction, cancellationToken);

            // Upsert from temp table using ON CONFLICT
            var upsertSql = BuildUpsertFromTempTableSql(tableName, tempTableName, keyColumns, allColumns, updateColumns);
            var affectedCount = await ExecuteAsync(connection, upsertSql, transaction: transaction, cancellationToken: cancellationToken);

            return affectedCount;
        }
        finally
        {
            // Drop temporary table
            await ExecuteAsync(connection, $"DROP TABLE IF EXISTS {EscapeIdentifier(tempTableName)}", transaction: transaction, cancellationToken: cancellationToken);
        }
    }

    /// <inheritdoc />
    public bool SupportsFeature(DatabaseFeature feature)
    {
        return feature switch
        {
            DatabaseFeature.Arrays => true,
            DatabaseFeature.Json => true,
            DatabaseFeature.UserDefinedTypes => true,
            DatabaseFeature.CompositeTypes => true,
            DatabaseFeature.BulkCopy => true,
            DatabaseFeature.CommonTableExpressions => true,
            DatabaseFeature.WindowFunctions => true,
            DatabaseFeature.RecursiveQueries => true,
            DatabaseFeature.FullTextSearch => true,
            DatabaseFeature.TableValuedParameters => false, // PostgreSQL uses composite types instead
            _ => false
        };
    }

    private string BuildParameterPlaceholders(object? parameters)
    {
        if (parameters == null)
            return string.Empty;

        var properties = parameters.GetType().GetProperties();
        return string.Join(", ", properties.Select(p => $"@{p.Name}"));
    }

    private string BuildCreateTempTableSql<T>(string tempTableName)
    {
        var properties = typeof(T).GetProperties();
        var columns = properties.Select(p => 
        {
            var columnName = EscapeIdentifier(p.GetPostgreSqlColumnName());
            var columnType = p.GetPostgreSqlTypeString();
            return $"{columnName} {columnType}";
        });

        return $"CREATE TEMP TABLE {EscapeIdentifier(tempTableName)} ({string.Join(", ", columns)})";
    }

    private string BuildUpdateFromTempTableSql(string tableName, string tempTableName, string[] keyColumns, string[] updateColumns)
    {
        var setClause = string.Join(", ", updateColumns.Select(col => 
            $"{EscapeIdentifier(col)} = temp.{EscapeIdentifier(col)}"));
        
        var whereClause = string.Join(" AND ", keyColumns.Select(col => 
            $"{EscapeIdentifier(tableName)}.{EscapeIdentifier(col)} = temp.{EscapeIdentifier(col)}"));

        return $@"
            UPDATE {EscapeIdentifier(tableName)} 
            SET {setClause}
            FROM {EscapeIdentifier(tempTableName)} temp
            WHERE {whereClause}";
    }

    private string BuildUpsertFromTempTableSql(string tableName, string tempTableName, string[] keyColumns, string[] allColumns, string[] updateColumns)
    {
        var columnsClause = string.Join(", ", allColumns.Select(EscapeIdentifier));
        var valuesClause = string.Join(", ", allColumns.Select(col => $"temp.{EscapeIdentifier(col)}"));
        var conflictClause = string.Join(", ", keyColumns.Select(EscapeIdentifier));
        var setClause = string.Join(", ", updateColumns.Select(col => 
            $"{EscapeIdentifier(col)} = EXCLUDED.{EscapeIdentifier(col)}"));

        return $@"
            INSERT INTO {EscapeIdentifier(tableName)} ({columnsClause})
            SELECT {valuesClause}
            FROM {EscapeIdentifier(tempTableName)} temp
            ON CONFLICT ({conflictClause})
            DO UPDATE SET {setClause}";
    }
}
