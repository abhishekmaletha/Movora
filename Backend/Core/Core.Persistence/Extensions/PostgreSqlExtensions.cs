using Core.Persistence.Attributes;
using Npgsql;
using NpgsqlTypes;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Reflection;

namespace Core.Persistence.Extensions;

/// <summary>
/// PostgreSQL-specific extensions
/// </summary>
public static class PostgreSqlExtensions
{
    /// <summary>
    /// Gets the PostgreSQL column name for a property
    /// </summary>
    public static string GetPostgreSqlColumnName(this PropertyInfo property)
    {
        var pgNameAttr = property.GetCustomAttribute<Attributes.PgNameAttribute>();
        if (pgNameAttr != null)
            return pgNameAttr.Name;

        var columnAttr = property.GetCustomAttribute<ColumnAttribute>();
        if (columnAttr != null && !string.IsNullOrEmpty(columnAttr.Name))
            return columnAttr.Name;

        // Convert to snake_case by default
        return property.Name.ToSnakeCase();
    }

    /// <summary>
    /// Gets the PostgreSQL type for a property
    /// </summary>
    public static NpgsqlDbType GetPostgreSqlType(this PropertyInfo property)
    {
        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        return Type.GetTypeCode(type) switch
        {
            TypeCode.Boolean => NpgsqlDbType.Boolean,
            TypeCode.Byte => NpgsqlDbType.Smallint,
            TypeCode.SByte => NpgsqlDbType.Smallint,
            TypeCode.Int16 => NpgsqlDbType.Smallint,
            TypeCode.UInt16 => NpgsqlDbType.Integer,
            TypeCode.Int32 => NpgsqlDbType.Integer,
            TypeCode.UInt32 => NpgsqlDbType.Bigint,
            TypeCode.Int64 => NpgsqlDbType.Bigint,
            TypeCode.UInt64 => NpgsqlDbType.Numeric,
            TypeCode.Single => NpgsqlDbType.Real,
            TypeCode.Double => NpgsqlDbType.Double,
            TypeCode.Decimal => NpgsqlDbType.Numeric,
            TypeCode.DateTime => NpgsqlDbType.Timestamp,
            TypeCode.String => NpgsqlDbType.Text,
            TypeCode.Char => NpgsqlDbType.Char,
            _ => GetComplexPostgreSqlType(type)
        };
    }

    /// <summary>
    /// Gets the PostgreSQL type string for DDL statements
    /// </summary>
    public static string GetPostgreSqlTypeString(this PropertyInfo property)
    {
        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        return Type.GetTypeCode(type) switch
        {
            TypeCode.Boolean => "BOOLEAN",
            TypeCode.Byte => "SMALLINT",
            TypeCode.SByte => "SMALLINT",
            TypeCode.Int16 => "SMALLINT",
            TypeCode.UInt16 => "INTEGER",
            TypeCode.Int32 => "INTEGER",
            TypeCode.UInt32 => "BIGINT",
            TypeCode.Int64 => "BIGINT",
            TypeCode.UInt64 => "NUMERIC",
            TypeCode.Single => "REAL",
            TypeCode.Double => "DOUBLE PRECISION",
            TypeCode.Decimal => "NUMERIC",
            TypeCode.DateTime => "TIMESTAMP",
            TypeCode.String => "TEXT",
            TypeCode.Char => "CHAR(1)",
            _ => GetComplexPostgreSqlTypeString(type)
        };
    }

    /// <summary>
    /// Checks if a PostgreSQL exception is retriable
    /// </summary>
    public static bool IsRetriableError(this PostgresException exception)
    {
        return exception.SqlState switch
        {
            "53300" => true, // too_many_connections
            "53400" => true, // configuration_limit_exceeded
            "08000" => true, // connection_exception
            "08003" => true, // connection_does_not_exist
            "08006" => true, // connection_failure
            "08001" => true, // sqlclient_unable_to_establish_sqlconnection
            "08004" => true, // sqlserver_rejected_establishment_of_sqlconnection
            "57P01" => true, // admin_shutdown
            "57P02" => true, // crash_shutdown
            "57P03" => true, // cannot_connect_now
            "25006" => true, // read_only_sql_transaction
            "40001" => true, // serialization_failure
            "40P01" => true, // deadlock_detected
            _ => false
        };
    }

    /// <summary>
    /// Creates a composite data table for PostgreSQL
    /// </summary>
    public static DataTable CreateCompositeTable<T>(this IEnumerable<T> collection)
    {
        var dataTable = new DataTable();
        var properties = typeof(T).GetProperties();

        // Add columns
        foreach (var property in properties)
        {
            var columnName = property.GetPostgreSqlColumnName();
            var columnType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            dataTable.Columns.Add(columnName, columnType);
        }

        // Add rows
        foreach (var item in collection)
        {
            var row = dataTable.NewRow();
            foreach (var property in properties)
            {
                var columnName = property.GetPostgreSqlColumnName();
                var value = property.GetValue(item);
                row[columnName] = value ?? DBNull.Value;
            }
            dataTable.Rows.Add(row);
        }

        return dataTable;
    }

    /// <summary>
    /// Creates a PostgreSQL array parameter
    /// </summary>
    public static object AsArrayParameter<T>(this IEnumerable<T> collection)
    {
        return collection.ToArray();
    }

    /// <summary>
    /// Creates a PostgreSQL composite type parameter
    /// </summary>
    public static object AsCompositeTypeParameter<T>(this IEnumerable<T> collection, string typeName)
    {
        var dataTable = collection.CreateCompositeTable();
        return new { Data = dataTable, TypeName = typeName };
    }

    /// <summary>
    /// Converts a string to snake_case
    /// </summary>
    public static string ToSnakeCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = new System.Text.StringBuilder();
        for (int i = 0; i < input.Length; i++)
        {
            if (char.IsUpper(input[i]))
            {
                if (i > 0)
                    result.Append('_');
                result.Append(char.ToLower(input[i]));
            }
            else
            {
                result.Append(input[i]);
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Gets the PostgreSQL composite attribute from a property
    /// </summary>
    public static PostgreSqlCompositeAttribute? GetPostgreSqlCompositeAttribute(this PropertyInfo property)
    {
        return property.GetCustomAttribute<PostgreSqlCompositeAttribute>();
    }

    /// <summary>
    /// Gets the PgName attribute from a property
    /// </summary>
    public static Attributes.PgNameAttribute? GetPgNameAttribute(this PropertyInfo property)
    {
        return property.GetCustomAttribute<Attributes.PgNameAttribute>();
    }

    private static NpgsqlDbType GetComplexPostgreSqlType(Type type)
    {
        if (type == typeof(Guid))
            return NpgsqlDbType.Uuid;
        
        if (type == typeof(DateTimeOffset))
            return NpgsqlDbType.TimestampTz;
        
        if (type == typeof(TimeSpan))
            return NpgsqlDbType.Interval;
        
        if (type == typeof(byte[]))
            return NpgsqlDbType.Bytea;
        
        if (type.IsArray)
            return NpgsqlDbType.Array;
        
        if (type.IsEnum)
            return NpgsqlDbType.Integer;

        // Default to text for unknown types
        return NpgsqlDbType.Text;
    }

    private static string GetComplexPostgreSqlTypeString(Type type)
    {
        if (type == typeof(Guid))
            return "UUID";
        
        if (type == typeof(DateTimeOffset))
            return "TIMESTAMP WITH TIME ZONE";
        
        if (type == typeof(TimeSpan))
            return "INTERVAL";
        
        if (type == typeof(byte[]))
            return "BYTEA";
        
        if (type.IsArray)
        {
            var elementType = type.GetElementType();
            var elementTypeString = GetElementTypeString(elementType);
            return $"{elementTypeString}[]";
        }
        
        if (type.IsEnum)
            return "INTEGER";

        // Default to text for unknown types
        return "TEXT";
    }

    private static string GetElementTypeString(Type? elementType)
    {
        if (elementType == null)
            return "TEXT";

        return Type.GetTypeCode(elementType) switch
        {
            TypeCode.Boolean => "BOOLEAN",
            TypeCode.Int32 => "INTEGER",
            TypeCode.Int64 => "BIGINT",
            TypeCode.String => "TEXT",
            TypeCode.DateTime => "TIMESTAMP",
            TypeCode.Decimal => "NUMERIC",
            _ => "TEXT"
        };
    }
}
