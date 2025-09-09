using NpgsqlTypes;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Core.Persistence.TypeMapping;

/// <summary>
/// Maps C# types to PostgreSQL types
/// </summary>
public class PostgreSqlTypeMapper
{
    private static readonly ConcurrentDictionary<Type, NpgsqlDbType> TypeMappings = new();
    private static readonly ConcurrentDictionary<Type, string> TypeStringMappings = new();

    static PostgreSqlTypeMapper()
    {
        InitializeDefaultMappings();
    }

    /// <summary>
    /// Gets the PostgreSQL type for a C# type
    /// </summary>
    /// <param name="type">C# type</param>
    /// <returns>PostgreSQL type</returns>
    public static NpgsqlDbType GetPostgreSqlType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        
        return TypeMappings.GetOrAdd(underlyingType, t => MapTypeToNpgsqlDbType(t));
    }

    /// <summary>
    /// Gets the PostgreSQL type string for a C# type
    /// </summary>
    /// <param name="type">C# type</param>
    /// <returns>PostgreSQL type string</returns>
    public static string GetPostgreSqlTypeString(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        
        return TypeStringMappings.GetOrAdd(underlyingType, t => MapTypeToString(t));
    }

    /// <summary>
    /// Registers a custom type mapping
    /// </summary>
    /// <param name="clrType">CLR type</param>
    /// <param name="npgsqlType">PostgreSQL type</param>
    /// <param name="typeString">PostgreSQL type string</param>
    public static void RegisterTypeMapping(Type clrType, NpgsqlDbType npgsqlType, string typeString)
    {
        TypeMappings.AddOrUpdate(clrType, npgsqlType, (key, oldValue) => npgsqlType);
        TypeStringMappings.AddOrUpdate(clrType, typeString, (key, oldValue) => typeString);
    }

    /// <summary>
    /// Checks if a type is supported
    /// </summary>
    /// <param name="type">Type to check</param>
    /// <returns>True if supported</returns>
    public static bool IsTypeSupported(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        return TypeMappings.ContainsKey(underlyingType) || CanMapType(underlyingType);
    }

    /// <summary>
    /// Gets all registered type mappings
    /// </summary>
    /// <returns>Dictionary of type mappings</returns>
    public static IReadOnlyDictionary<Type, NpgsqlDbType> GetAllTypeMappings()
    {
        return TypeMappings.ToImmutableDictionary();
    }

    /// <summary>
    /// Gets all registered type string mappings
    /// </summary>
    /// <returns>Dictionary of type string mappings</returns>
    public static IReadOnlyDictionary<Type, string> GetAllTypeStringMappings()
    {
        return TypeStringMappings.ToImmutableDictionary();
    }

    private static void InitializeDefaultMappings()
    {
        // Basic types
        TypeMappings[typeof(bool)] = NpgsqlDbType.Boolean;
        TypeMappings[typeof(byte)] = NpgsqlDbType.Smallint;
        TypeMappings[typeof(sbyte)] = NpgsqlDbType.Smallint;
        TypeMappings[typeof(short)] = NpgsqlDbType.Smallint;
        TypeMappings[typeof(ushort)] = NpgsqlDbType.Integer;
        TypeMappings[typeof(int)] = NpgsqlDbType.Integer;
        TypeMappings[typeof(uint)] = NpgsqlDbType.Bigint;
        TypeMappings[typeof(long)] = NpgsqlDbType.Bigint;
        TypeMappings[typeof(ulong)] = NpgsqlDbType.Numeric;
        TypeMappings[typeof(float)] = NpgsqlDbType.Real;
        TypeMappings[typeof(double)] = NpgsqlDbType.Double;
        TypeMappings[typeof(decimal)] = NpgsqlDbType.Numeric;
        TypeMappings[typeof(string)] = NpgsqlDbType.Text;
        TypeMappings[typeof(char)] = NpgsqlDbType.Char;
        TypeMappings[typeof(Guid)] = NpgsqlDbType.Uuid;
        TypeMappings[typeof(DateTime)] = NpgsqlDbType.Timestamp;
        TypeMappings[typeof(DateTimeOffset)] = NpgsqlDbType.TimestampTz;
        TypeMappings[typeof(TimeSpan)] = NpgsqlDbType.Interval;
        TypeMappings[typeof(byte[])] = NpgsqlDbType.Bytea;

        // String mappings
        TypeStringMappings[typeof(bool)] = "BOOLEAN";
        TypeStringMappings[typeof(byte)] = "SMALLINT";
        TypeStringMappings[typeof(sbyte)] = "SMALLINT";
        TypeStringMappings[typeof(short)] = "SMALLINT";
        TypeStringMappings[typeof(ushort)] = "INTEGER";
        TypeStringMappings[typeof(int)] = "INTEGER";
        TypeStringMappings[typeof(uint)] = "BIGINT";
        TypeStringMappings[typeof(long)] = "BIGINT";
        TypeStringMappings[typeof(ulong)] = "NUMERIC";
        TypeStringMappings[typeof(float)] = "REAL";
        TypeStringMappings[typeof(double)] = "DOUBLE PRECISION";
        TypeStringMappings[typeof(decimal)] = "NUMERIC";
        TypeStringMappings[typeof(string)] = "TEXT";
        TypeStringMappings[typeof(char)] = "CHAR(1)";
        TypeStringMappings[typeof(Guid)] = "UUID";
        TypeStringMappings[typeof(DateTime)] = "TIMESTAMP";
        TypeStringMappings[typeof(DateTimeOffset)] = "TIMESTAMP WITH TIME ZONE";
        TypeStringMappings[typeof(TimeSpan)] = "INTERVAL";
        TypeStringMappings[typeof(byte[])] = "BYTEA";

        // JSON types
        RegisterJsonTypes();

        // Array types
        RegisterArrayTypes();
    }

    private static void RegisterJsonTypes()
    {
        // Common JSON types - use JSONB by default for better performance
        TypeMappings[typeof(object)] = NpgsqlDbType.Jsonb;
        TypeStringMappings[typeof(object)] = "JSONB";
    }

    private static void RegisterArrayTypes()
    {
        // Common array types
        TypeMappings[typeof(int[])] = NpgsqlDbType.Array | NpgsqlDbType.Integer;
        TypeMappings[typeof(string[])] = NpgsqlDbType.Array | NpgsqlDbType.Text;
        TypeMappings[typeof(long[])] = NpgsqlDbType.Array | NpgsqlDbType.Bigint;
        TypeMappings[typeof(bool[])] = NpgsqlDbType.Array | NpgsqlDbType.Boolean;
        TypeMappings[typeof(decimal[])] = NpgsqlDbType.Array | NpgsqlDbType.Numeric;
        TypeMappings[typeof(DateTime[])] = NpgsqlDbType.Array | NpgsqlDbType.Timestamp;
        TypeMappings[typeof(Guid[])] = NpgsqlDbType.Array | NpgsqlDbType.Uuid;

        TypeStringMappings[typeof(int[])] = "INTEGER[]";
        TypeStringMappings[typeof(string[])] = "TEXT[]";
        TypeStringMappings[typeof(long[])] = "BIGINT[]";
        TypeStringMappings[typeof(bool[])] = "BOOLEAN[]";
        TypeStringMappings[typeof(decimal[])] = "NUMERIC[]";
        TypeStringMappings[typeof(DateTime[])] = "TIMESTAMP[]";
        TypeStringMappings[typeof(Guid[])] = "UUID[]";
    }

    private static NpgsqlDbType MapTypeToNpgsqlDbType(Type type)
    {
        if (type.IsEnum)
            return NpgsqlDbType.Integer;

        if (type.IsArray)
        {
            var elementType = type.GetElementType();
            if (elementType != null && TypeMappings.TryGetValue(elementType, out var elementDbType))
            {
                return NpgsqlDbType.Array | elementDbType;
            }
            return NpgsqlDbType.Array | NpgsqlDbType.Text;
        }

        if (type.IsGenericType)
        {
            var genericType = type.GetGenericTypeDefinition();
            
            if (genericType == typeof(List<>) || genericType == typeof(IList<>) || 
                genericType == typeof(ICollection<>) || genericType == typeof(IEnumerable<>))
            {
                var elementType = type.GetGenericArguments()[0];
                if (TypeMappings.TryGetValue(elementType, out var elementDbType))
                {
                    return NpgsqlDbType.Array | elementDbType;
                }
                return NpgsqlDbType.Array | NpgsqlDbType.Text;
            }

            if (genericType == typeof(Dictionary<,>) || genericType == typeof(IDictionary<,>))
            {
                return NpgsqlDbType.Jsonb;
            }
        }

        // Default to text for unknown types
        return NpgsqlDbType.Text;
    }

    private static string MapTypeToString(Type type)
    {
        if (type.IsEnum)
            return "INTEGER";

        if (type.IsArray)
        {
            var elementType = type.GetElementType();
            if (elementType != null && TypeStringMappings.TryGetValue(elementType, out var elementTypeString))
            {
                return $"{elementTypeString}[]";
            }
            return "TEXT[]";
        }

        if (type.IsGenericType)
        {
            var genericType = type.GetGenericTypeDefinition();
            
            if (genericType == typeof(List<>) || genericType == typeof(IList<>) || 
                genericType == typeof(ICollection<>) || genericType == typeof(IEnumerable<>))
            {
                var elementType = type.GetGenericArguments()[0];
                if (TypeStringMappings.TryGetValue(elementType, out var elementTypeString))
                {
                    return $"{elementTypeString}[]";
                }
                return "TEXT[]";
            }

            if (genericType == typeof(Dictionary<,>) || genericType == typeof(IDictionary<,>))
            {
                return "JSONB";
            }
        }

        // Default to text for unknown types
        return "TEXT";
    }

    private static bool CanMapType(Type type)
    {
        return type.IsEnum || 
               type.IsArray || 
               (type.IsGenericType && (
                   type.GetGenericTypeDefinition() == typeof(List<>) ||
                   type.GetGenericTypeDefinition() == typeof(IList<>) ||
                   type.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                   type.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
                   type.GetGenericTypeDefinition() == typeof(Dictionary<,>) ||
                   type.GetGenericTypeDefinition() == typeof(IDictionary<,>)
               ));
    }
}

/// <summary>
/// Composite type manager for PostgreSQL
/// </summary>
public class CompositeTypeManager
{
    private static readonly ConcurrentDictionary<string, Type> RegisteredTypes = new();
    private static readonly ConcurrentDictionary<Type, string> TypeToNameMapping = new();

    /// <summary>
    /// Registers a composite type mapping
    /// </summary>
    /// <typeparam name="T">CLR type</typeparam>
    /// <param name="typeName">PostgreSQL composite type name</param>
    public void RegisterCompositeType<T>(string typeName)
    {
        var clrType = typeof(T);
        RegisteredTypes.AddOrUpdate(typeName, clrType, (key, oldValue) => clrType);
        TypeToNameMapping.AddOrUpdate(clrType, typeName, (key, oldValue) => typeName);
    }

    /// <summary>
    /// Gets the CLR type for a composite type name
    /// </summary>
    /// <param name="typeName">PostgreSQL composite type name</param>
    /// <returns>CLR type or null if not found</returns>
    public Type? GetClrType(string typeName)
    {
        return RegisteredTypes.TryGetValue(typeName, out var type) ? type : null;
    }

    /// <summary>
    /// Gets the PostgreSQL type name for a CLR type
    /// </summary>
    /// <param name="clrType">CLR type</param>
    /// <returns>PostgreSQL type name or null if not found</returns>
    public string? GetPostgreSqlTypeName(Type clrType)
    {
        return TypeToNameMapping.TryGetValue(clrType, out var typeName) ? typeName : null;
    }

    /// <summary>
    /// Checks if a composite type is registered
    /// </summary>
    /// <param name="typeName">PostgreSQL composite type name</param>
    /// <returns>True if registered</returns>
    public bool IsRegistered(string typeName)
    {
        return RegisteredTypes.ContainsKey(typeName);
    }

    /// <summary>
    /// Checks if a CLR type is registered as a composite type
    /// </summary>
    /// <param name="clrType">CLR type</param>
    /// <returns>True if registered</returns>
    public bool IsRegistered(Type clrType)
    {
        return TypeToNameMapping.ContainsKey(clrType);
    }

    /// <summary>
    /// Gets all registered composite types
    /// </summary>
    /// <returns>Dictionary of type name to CLR type mappings</returns>
    public IReadOnlyDictionary<string, Type> GetAllRegisteredTypes()
    {
        return RegisteredTypes.ToImmutableDictionary();
    }

    /// <summary>
    /// Unregisters a composite type
    /// </summary>
    /// <param name="typeName">PostgreSQL composite type name</param>
    /// <returns>True if successfully unregistered</returns>
    public bool UnregisterCompositeType(string typeName)
    {
        if (RegisteredTypes.TryRemove(typeName, out var clrType))
        {
            TypeToNameMapping.TryRemove(clrType, out _);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Clears all registered composite types
    /// </summary>
    public void Clear()
    {
        RegisteredTypes.Clear();
        TypeToNameMapping.Clear();
    }
}
