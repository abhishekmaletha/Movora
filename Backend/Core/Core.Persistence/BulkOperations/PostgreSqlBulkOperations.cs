using Core.Persistence.Configuration;
using Core.Persistence.Extensions;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using System.Data;
using System.Text;

namespace Core.Persistence.BulkOperations;

/// <summary>
/// PostgreSQL bulk operations implementation
/// </summary>
public class PostgreSqlBulkOperations
{
    private readonly ILogger<PostgreSqlBulkOperations> _logger;

    /// <summary>
    /// Initializes a new instance of PostgreSqlBulkOperations
    /// </summary>
    public PostgreSqlBulkOperations(ILogger<PostgreSqlBulkOperations> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Performs bulk insert using PostgreSQL COPY command
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="connection">Database connection</param>
    /// <param name="tableName">Table name</param>
    /// <param name="data">Data to insert</param>
    /// <param name="options">Bulk operation options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of rows inserted</returns>
    public async Task<long> BulkInsertAsync<T>(
        IDbConnection connection,
        string tableName,
        IEnumerable<T> data,
        BulkOperationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (connection is not NpgsqlConnection npgsqlConnection)
            throw new ArgumentException("Connection must be NpgsqlConnection for PostgreSQL bulk operations", nameof(connection));

        options ??= new BulkOperationOptions();
        var dataList = data.ToList();
        
        if (!dataList.Any())
        {
            _logger.LogDebug("No data provided for bulk insert to {TableName}", tableName);
            return 0;
        }

        _logger.LogDebug("Starting bulk insert to {TableName} with {Count} records", tableName, dataList.Count);

        var properties = typeof(T).GetProperties();
        var columnNames = properties.Select(p => p.GetPostgreSqlColumnName()).ToArray();
        var copyCommand = $"COPY {EscapeIdentifier(tableName)} ({string.Join(", ", columnNames.Select(EscapeIdentifier))}) FROM STDIN (FORMAT BINARY)";

        long insertedCount = 0;
        var batchCount = 0;
        var totalBatches = (int)Math.Ceiling((double)dataList.Count / options.BatchSize);

        try
        {
            using var writer = npgsqlConnection.BeginBinaryImport(copyCommand);
            
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

                // Log progress
                if (options.LogProgress && insertedCount % options.ProgressInterval == 0)
                {
                    _logger.LogInformation("Bulk insert progress: {InsertedCount}/{TotalCount} records processed", 
                        insertedCount, dataList.Count);
                }

                // Handle batching
                if (insertedCount % options.BatchSize == 0)
                {
                    batchCount++;
                    if (options.LogProgress)
                    {
                        _logger.LogInformation("Completed batch {BatchCount}/{TotalBatches}", batchCount, totalBatches);
                    }
                }
            }

            await writer.CompleteAsync(cancellationToken);
            
            _logger.LogInformation("Bulk insert completed successfully. Inserted {Count} records into {TableName}", 
                insertedCount, tableName);
            
            return insertedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk insert failed for table {TableName} after inserting {Count} records", 
                tableName, insertedCount);
            
            if (!options.ContinueOnError)
                throw;
            
            return insertedCount;
        }
    }

    /// <summary>
    /// Performs bulk insert using COPY with CSV format
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="connection">Database connection</param>
    /// <param name="tableName">Table name</param>
    /// <param name="data">Data to insert</param>
    /// <param name="options">Bulk operation options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of rows inserted</returns>
    public async Task<long> BulkInsertWithCsvAsync<T>(
        IDbConnection connection,
        string tableName,
        IEnumerable<T> data,
        BulkOperationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (connection is not NpgsqlConnection npgsqlConnection)
            throw new ArgumentException("Connection must be NpgsqlConnection for PostgreSQL bulk operations", nameof(connection));

        options ??= new BulkOperationOptions();
        var dataList = data.ToList();
        
        if (!dataList.Any())
            return 0;

        var properties = typeof(T).GetProperties();
        var columnNames = properties.Select(p => p.GetPostgreSqlColumnName()).ToArray();
        var copyCommand = $"COPY {EscapeIdentifier(tableName)} ({string.Join(", ", columnNames.Select(EscapeIdentifier))}) FROM STDIN (FORMAT CSV)";

        long insertedCount = 0;

        try
        {
            using var writer = npgsqlConnection.BeginTextImport(copyCommand);
            
            foreach (var item in dataList)
            {
                var csvLine = BuildCsvLine(item, properties);
                await writer.WriteLineAsync(csvLine);
                insertedCount++;

                if (options.LogProgress && insertedCount % options.ProgressInterval == 0)
                {
                    _logger.LogInformation("CSV bulk insert progress: {InsertedCount}/{TotalCount} records processed", 
                        insertedCount, dataList.Count);
                }
            }

            _logger.LogInformation("CSV bulk insert completed. Inserted {Count} records into {TableName}", 
                insertedCount, tableName);
            
            return insertedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CSV bulk insert failed for table {TableName} after inserting {Count} records", 
                tableName, insertedCount);
            throw;
        }
    }

    /// <summary>
    /// Performs bulk upsert operation
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="connection">Database connection</param>
    /// <param name="tableName">Table name</param>
    /// <param name="data">Data to upsert</param>
    /// <param name="keyColumns">Key columns for conflict resolution</param>
    /// <param name="options">Bulk operation options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of rows affected</returns>
    public async Task<long> BulkUpsertAsync<T>(
        IDbConnection connection,
        string tableName,
        IEnumerable<T> data,
        string[] keyColumns,
        BulkOperationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (connection is not NpgsqlConnection npgsqlConnection)
            throw new ArgumentException("Connection must be NpgsqlConnection for PostgreSQL bulk operations", nameof(connection));

        options ??= new BulkOperationOptions();
        var dataList = data.ToList();
        
        if (!dataList.Any())
            return 0;

        var tempTableName = $"temp_{tableName}_{Guid.NewGuid():N}";
        
        try
        {
            _logger.LogDebug("Starting bulk upsert to {TableName} using temporary table {TempTableName}", 
                tableName, tempTableName);

            // Create temporary table
            await CreateTempTableAsync<T>(npgsqlConnection, tempTableName, cancellationToken);

            // Bulk insert into temporary table
            await BulkInsertAsync(npgsqlConnection, tempTableName, dataList, options, cancellationToken);

            // Perform upsert from temporary table
            var affectedRows = await UpsertFromTempTableAsync<T>(npgsqlConnection, tableName, tempTableName, 
                keyColumns, cancellationToken);

            _logger.LogInformation("Bulk upsert completed. Affected {Count} rows in {TableName}", 
                affectedRows, tableName);

            return affectedRows;
        }
        finally
        {
            // Clean up temporary table
            await DropTempTableAsync(npgsqlConnection, tempTableName, cancellationToken);
        }
    }

    /// <summary>
    /// Performs bulk update operation
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="connection">Database connection</param>
    /// <param name="tableName">Table name</param>
    /// <param name="data">Data to update</param>
    /// <param name="keyColumns">Key columns for matching</param>
    /// <param name="options">Bulk operation options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of rows updated</returns>
    public async Task<long> BulkUpdateAsync<T>(
        IDbConnection connection,
        string tableName,
        IEnumerable<T> data,
        string[] keyColumns,
        BulkOperationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (connection is not NpgsqlConnection npgsqlConnection)
            throw new ArgumentException("Connection must be NpgsqlConnection for PostgreSQL bulk operations", nameof(connection));

        options ??= new BulkOperationOptions();
        var dataList = data.ToList();
        
        if (!dataList.Any())
            return 0;

        var tempTableName = $"temp_{tableName}_{Guid.NewGuid():N}";
        
        try
        {
            _logger.LogDebug("Starting bulk update to {TableName} using temporary table {TempTableName}", 
                tableName, tempTableName);

            // Create temporary table
            await CreateTempTableAsync<T>(npgsqlConnection, tempTableName, cancellationToken);

            // Bulk insert into temporary table
            await BulkInsertAsync(npgsqlConnection, tempTableName, dataList, options, cancellationToken);

            // Perform update from temporary table
            var updatedRows = await UpdateFromTempTableAsync<T>(npgsqlConnection, tableName, tempTableName, 
                keyColumns, cancellationToken);

            _logger.LogInformation("Bulk update completed. Updated {Count} rows in {TableName}", 
                updatedRows, tableName);

            return updatedRows;
        }
        finally
        {
            // Clean up temporary table
            await DropTempTableAsync(npgsqlConnection, tempTableName, cancellationToken);
        }
    }

    private async Task CreateTempTableAsync<T>(NpgsqlConnection connection, string tempTableName, CancellationToken cancellationToken)
    {
        var properties = typeof(T).GetProperties();
        var columns = properties.Select(p => 
        {
            var columnName = EscapeIdentifier(p.GetPostgreSqlColumnName());
            var columnType = p.GetPostgreSqlTypeString();
            return $"{columnName} {columnType}";
        });

        var createTableSql = $"CREATE TEMP TABLE {EscapeIdentifier(tempTableName)} ({string.Join(", ", columns)})";
        
        using var command = new NpgsqlCommand(createTableSql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
        
        _logger.LogDebug("Created temporary table {TempTableName}", tempTableName);
    }

    private async Task<long> UpsertFromTempTableAsync<T>(NpgsqlConnection connection, string tableName, 
        string tempTableName, string[] keyColumns, CancellationToken cancellationToken)
    {
        var properties = typeof(T).GetProperties();
        var allColumns = properties.Select(p => p.GetPostgreSqlColumnName()).ToArray();
        var updateColumns = properties.Where(p => !keyColumns.Contains(p.Name)).Select(p => p.GetPostgreSqlColumnName()).ToArray();
        
        var columnsClause = string.Join(", ", allColumns.Select(EscapeIdentifier));
        var valuesClause = string.Join(", ", allColumns.Select(col => $"temp.{EscapeIdentifier(col)}"));
        var conflictClause = string.Join(", ", keyColumns.Select(EscapeIdentifier));
        var setClause = string.Join(", ", updateColumns.Select(col => 
            $"{EscapeIdentifier(col)} = EXCLUDED.{EscapeIdentifier(col)}"));

        var upsertSql = $@"
            INSERT INTO {EscapeIdentifier(tableName)} ({columnsClause})
            SELECT {valuesClause}
            FROM {EscapeIdentifier(tempTableName)} temp
            ON CONFLICT ({conflictClause})
            DO UPDATE SET {setClause}";

        using var command = new NpgsqlCommand(upsertSql, connection);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<long> UpdateFromTempTableAsync<T>(NpgsqlConnection connection, string tableName, 
        string tempTableName, string[] keyColumns, CancellationToken cancellationToken)
    {
        var properties = typeof(T).GetProperties();
        var updateColumns = properties.Where(p => !keyColumns.Contains(p.Name)).Select(p => p.GetPostgreSqlColumnName()).ToArray();
        
        var setClause = string.Join(", ", updateColumns.Select(col => 
            $"{EscapeIdentifier(col)} = temp.{EscapeIdentifier(col)}"));
        
        var whereClause = string.Join(" AND ", keyColumns.Select(col => 
            $"{EscapeIdentifier(tableName)}.{EscapeIdentifier(col)} = temp.{EscapeIdentifier(col)}"));

        var updateSql = $@"
            UPDATE {EscapeIdentifier(tableName)} 
            SET {setClause}
            FROM {EscapeIdentifier(tempTableName)} temp
            WHERE {whereClause}";

        using var command = new NpgsqlCommand(updateSql, connection);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task DropTempTableAsync(NpgsqlConnection connection, string tempTableName, CancellationToken cancellationToken)
    {
        try
        {
            var dropTableSql = $"DROP TABLE IF EXISTS {EscapeIdentifier(tempTableName)}";
            using var command = new NpgsqlCommand(dropTableSql, connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
            
            _logger.LogDebug("Dropped temporary table {TempTableName}", tempTableName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to drop temporary table {TempTableName}", tempTableName);
        }
    }

    private string BuildCsvLine<T>(T item, System.Reflection.PropertyInfo[] properties)
    {
        var values = new List<string>();
        
        foreach (var property in properties)
        {
            var value = property.GetValue(item);
            var csvValue = value switch
            {
                null => "",
                string str => $"\"{str.Replace("\"", "\"\"")}\"",
                DateTime dt => $"\"{dt:yyyy-MM-dd HH:mm:ss}\"",
                DateTimeOffset dto => $"\"{dto:yyyy-MM-dd HH:mm:ss zzz}\"",
                bool b => b ? "true" : "false",
                _ => value.ToString() ?? ""
            };
            
            values.Add(csvValue);
        }
        
        return string.Join(",", values);
    }

    private static string EscapeIdentifier(string identifier)
    {
        return $"\"{identifier.Replace("\"", "\"\"")}\"";
    }
}
