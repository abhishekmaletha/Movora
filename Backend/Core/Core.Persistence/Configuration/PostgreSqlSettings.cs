namespace Core.Persistence.Configuration;

/// <summary>
/// PostgreSQL database settings
/// </summary>
public class PostgreSqlSettings
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "PostgreSQL";

    /// <summary>
    /// PostgreSQL connection string
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Maximum retry count for transient errors
    /// </summary>
    public int? MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// Maximum retry delay duration in seconds
    /// </summary>
    public int? MaxRetryDelayDuration { get; set; } = 30;

    /// <summary>
    /// Base retry delay in milliseconds
    /// </summary>
    public int BaseRetryDelayMilliseconds { get; set; } = 1000;

    /// <summary>
    /// Whether to use exponential backoff for retries
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;

    /// <summary>
    /// Enable PostgreSQL array support
    /// </summary>
    public bool EnableArraySupport { get; set; } = true;

    /// <summary>
    /// Enable PostgreSQL composite types
    /// </summary>
    public bool EnableCompositeTypes { get; set; } = true;

    /// <summary>
    /// Enable bulk operations
    /// </summary>
    public bool EnableBulkOperations { get; set; } = true;

    /// <summary>
    /// Enable connection pooling
    /// </summary>
    public bool EnableConnectionPooling { get; set; } = true;

    /// <summary>
    /// Minimum pool size
    /// </summary>
    public int MinPoolSize { get; set; } = 1;

    /// <summary>
    /// Maximum pool size
    /// </summary>
    public int MaxPoolSize { get; set; } = 100;

    /// <summary>
    /// Connection lifetime in seconds
    /// </summary>
    public int ConnectionLifetime { get; set; } = 3600; // 1 hour

    /// <summary>
    /// Command timeout in seconds
    /// </summary>
    public int CommandTimeout { get; set; } = 30;

    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int ConnectionTimeout { get; set; } = 15;

    /// <summary>
    /// Composite type mappings (Type Name -> PostgreSQL Type Name)
    /// </summary>
    public Dictionary<string, string> CompositeTypeMappings { get; set; } = new();

    /// <summary>
    /// Custom type mappings
    /// </summary>
    public Dictionary<string, string> CustomTypeMappings { get; set; } = new();

    /// <summary>
    /// Whether to enable detailed logging
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Whether to log SQL parameters
    /// </summary>
    public bool LogParameters { get; set; } = false;

    /// <summary>
    /// Whether to log slow queries
    /// </summary>
    public bool LogSlowQueries { get; set; } = true;

    /// <summary>
    /// Slow query threshold in milliseconds
    /// </summary>
    public int SlowQueryThreshold { get; set; } = 1000;

    /// <summary>
    /// SSL mode for connections
    /// </summary>
    public string SslMode { get; set; } = "Prefer";

    /// <summary>
    /// Trust server certificate
    /// </summary>
    public bool TrustServerCertificate { get; set; } = false;

    /// <summary>
    /// Server compatibility mode
    /// </summary>
    public string ServerCompatibilityMode { get; set; } = "None";

    /// <summary>
    /// Whether to include error details in exceptions
    /// </summary>
    public bool IncludeErrorDetails { get; set; } = false;

    /// <summary>
    /// Load balance hosts
    /// </summary>
    public bool LoadBalanceHosts { get; set; } = false;

    /// <summary>
    /// Target session attributes
    /// </summary>
    public string TargetSessionAttributes { get; set; } = "any";
}

/// <summary>
/// PostgreSQL operation options
/// </summary>
public class PostgreSqlOptions
{
    /// <summary>
    /// Dynamic parameters for the operation
    /// </summary>
    public object? Parameters { get; set; }

    /// <summary>
    /// Function name to execute
    /// </summary>
    public string? FunctionName { get; set; }

    /// <summary>
    /// Schema name (default: public)
    /// </summary>
    public string SchemaName { get; set; } = "public";

    /// <summary>
    /// Whether to use composite types
    /// </summary>
    public bool UseCompositeTypes { get; set; } = false;

    /// <summary>
    /// Command timeout in seconds
    /// </summary>
    public int? CommandTimeout { get; set; }

    /// <summary>
    /// Whether to use prepared statements
    /// </summary>
    public bool UsePreparedStatements { get; set; } = false;

    /// <summary>
    /// Isolation level for transactions
    /// </summary>
    public System.Data.IsolationLevel? IsolationLevel { get; set; }

    /// <summary>
    /// Whether to auto-commit transactions
    /// </summary>
    public bool AutoCommit { get; set; } = true;

    /// <summary>
    /// Bulk operation batch size
    /// </summary>
    public int BulkBatchSize { get; set; } = 1000;

    /// <summary>
    /// Whether to use binary copy for bulk operations
    /// </summary>
    public bool UseBinaryCopy { get; set; } = true;

    /// <summary>
    /// Additional connection parameters
    /// </summary>
    public Dictionary<string, string> ConnectionParameters { get; set; } = new();

    /// <summary>
    /// Custom type handlers
    /// </summary>
    public Dictionary<Type, object> TypeHandlers { get; set; } = new();
}

/// <summary>
/// Database connection options
/// </summary>
public class DatabaseConnectionOptions
{
    /// <summary>
    /// Database provider name
    /// </summary>
    public string ProviderName { get; set; } = "PostgreSQL";

    /// <summary>
    /// Connection string
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Whether to enable connection monitoring
    /// </summary>
    public bool EnableMonitoring { get; set; } = true;

    /// <summary>
    /// Health check interval in seconds
    /// </summary>
    public int HealthCheckInterval { get; set; } = 30;

    /// <summary>
    /// Health check timeout in seconds
    /// </summary>
    public int HealthCheckTimeout { get; set; } = 5;

    /// <summary>
    /// Whether to enable automatic reconnection
    /// </summary>
    public bool EnableAutoReconnect { get; set; } = true;

    /// <summary>
    /// Maximum connection attempts
    /// </summary>
    public int MaxConnectionAttempts { get; set; } = 3;

    /// <summary>
    /// Connection retry delay in milliseconds
    /// </summary>
    public int ConnectionRetryDelay { get; set; } = 1000;
}

/// <summary>
/// Bulk operation options
/// </summary>
public class BulkOperationOptions
{
    /// <summary>
    /// Batch size for bulk operations
    /// </summary>
    public int BatchSize { get; set; } = 1000;

    /// <summary>
    /// Whether to use transactions for bulk operations
    /// </summary>
    public bool UseTransaction { get; set; } = true;

    /// <summary>
    /// Whether to continue on error
    /// </summary>
    public bool ContinueOnError { get; set; } = false;

    /// <summary>
    /// Timeout for bulk operations in seconds
    /// </summary>
    public int BulkTimeout { get; set; } = 300; // 5 minutes

    /// <summary>
    /// Whether to validate data before bulk operations
    /// </summary>
    public bool ValidateData { get; set; } = true;

    /// <summary>
    /// Whether to log bulk operation progress
    /// </summary>
    public bool LogProgress { get; set; } = true;

    /// <summary>
    /// Progress reporting interval (number of records)
    /// </summary>
    public int ProgressInterval { get; set; } = 10000;

    /// <summary>
    /// Whether to use parallel processing for large datasets
    /// </summary>
    public bool UseParallelProcessing { get; set; } = false;

    /// <summary>
    /// Maximum degree of parallelism
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
}
