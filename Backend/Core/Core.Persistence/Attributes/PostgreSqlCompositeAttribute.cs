namespace Core.Persistence.Attributes;

/// <summary>
/// Attribute for PostgreSQL composite type mapping
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class PostgreSqlCompositeAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of PostgreSqlCompositeAttribute
    /// </summary>
    /// <param name="position">Position in the composite type</param>
    /// <param name="name">Name in the composite type (optional)</param>
    public PostgreSqlCompositeAttribute(int position, string? name = null)
    {
        Position = position;
        Name = name;
    }

    /// <summary>
    /// Position of the property in the composite type
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Name of the property in the composite type
    /// </summary>
    public string? Name { get; set; }
}

/// <summary>
/// Attribute for PostgreSQL naming conventions
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
public class PgNameAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of PgNameAttribute
    /// </summary>
    /// <param name="name">PostgreSQL name</param>
    public PgNameAttribute(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    /// <summary>
    /// PostgreSQL name
    /// </summary>
    public string Name { get; set; }
}

/// <summary>
/// Attribute for PostgreSQL table mapping
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class PgTableAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of PgTableAttribute
    /// </summary>
    /// <param name="tableName">Table name</param>
    /// <param name="schemaName">Schema name (optional, defaults to "public")</param>
    public PgTableAttribute(string tableName, string? schemaName = null)
    {
        TableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
        SchemaName = schemaName ?? "public";
    }

    /// <summary>
    /// Table name
    /// </summary>
    public string TableName { get; set; }

    /// <summary>
    /// Schema name
    /// </summary>
    public string SchemaName { get; set; }

    /// <summary>
    /// Gets the fully qualified table name
    /// </summary>
    public string QualifiedName => $"{SchemaName}.{TableName}";
}

/// <summary>
/// Attribute for PostgreSQL column mapping
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class PgColumnAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of PgColumnAttribute
    /// </summary>
    /// <param name="columnName">Column name</param>
    /// <param name="dbType">PostgreSQL data type (optional)</param>
    public PgColumnAttribute(string columnName, string? dbType = null)
    {
        ColumnName = columnName ?? throw new ArgumentNullException(nameof(columnName));
        DbType = dbType;
    }

    /// <summary>
    /// Column name
    /// </summary>
    public string ColumnName { get; set; }

    /// <summary>
    /// PostgreSQL data type
    /// </summary>
    public string? DbType { get; set; }

    /// <summary>
    /// Whether the column is nullable
    /// </summary>
    public bool IsNullable { get; set; } = true;

    /// <summary>
    /// Whether the column is a primary key
    /// </summary>
    public bool IsPrimaryKey { get; set; } = false;

    /// <summary>
    /// Whether the column is auto-generated
    /// </summary>
    public bool IsGenerated { get; set; } = false;
}

/// <summary>
/// Attribute for PostgreSQL array columns
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class PgArrayAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of PgArrayAttribute
    /// </summary>
    /// <param name="elementType">Array element type</param>
    public PgArrayAttribute(Type elementType)
    {
        ElementType = elementType ?? throw new ArgumentNullException(nameof(elementType));
    }

    /// <summary>
    /// Array element type
    /// </summary>
    public Type ElementType { get; set; }

    /// <summary>
    /// Maximum array dimensions
    /// </summary>
    public int Dimensions { get; set; } = 1;
}

/// <summary>
/// Attribute for PostgreSQL JSON columns
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class PgJsonAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of PgJsonAttribute
    /// </summary>
    /// <param name="useJsonb">Whether to use JSONB instead of JSON (default: true)</param>
    public PgJsonAttribute(bool useJsonb = true)
    {
        UseJsonb = useJsonb;
    }

    /// <summary>
    /// Whether to use JSONB instead of JSON
    /// </summary>
    public bool UseJsonb { get; set; }

    /// <summary>
    /// Gets the PostgreSQL type name
    /// </summary>
    public string TypeName => UseJsonb ? "JSONB" : "JSON";
}

/// <summary>
/// Attribute for PostgreSQL function parameters
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class PgParameterAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of PgParameterAttribute
    /// </summary>
    /// <param name="parameterName">Parameter name</param>
    /// <param name="direction">Parameter direction (default: Input)</param>
    public PgParameterAttribute(string parameterName, ParameterDirection direction = ParameterDirection.Input)
    {
        ParameterName = parameterName ?? throw new ArgumentNullException(nameof(parameterName));
        Direction = direction;
    }

    /// <summary>
    /// Parameter name
    /// </summary>
    public string ParameterName { get; set; }

    /// <summary>
    /// Parameter direction
    /// </summary>
    public ParameterDirection Direction { get; set; }

    /// <summary>
    /// Parameter size (for variable-length types)
    /// </summary>
    public int Size { get; set; } = -1;

    /// <summary>
    /// Parameter precision (for numeric types)
    /// </summary>
    public byte Precision { get; set; } = 0;

    /// <summary>
    /// Parameter scale (for numeric types)
    /// </summary>
    public byte Scale { get; set; } = 0;
}

/// <summary>
/// Parameter direction enumeration
/// </summary>
public enum ParameterDirection
{
    /// <summary>
    /// Input parameter
    /// </summary>
    Input,

    /// <summary>
    /// Output parameter
    /// </summary>
    Output,

    /// <summary>
    /// Input/Output parameter
    /// </summary>
    InputOutput,

    /// <summary>
    /// Return value
    /// </summary>
    ReturnValue
}
