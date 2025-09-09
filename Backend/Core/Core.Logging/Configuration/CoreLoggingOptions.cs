using Core.Logging.Models;

namespace Core.Logging.Configuration;

/// <summary>
/// Main configuration options for Core.Logging
/// </summary>
public class CoreLoggingOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "CoreLogging";

    /// <summary>
    /// Minimum log level
    /// </summary>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Application name for logging context
    /// </summary>
    public string ApplicationName { get; set; } = "Application";

    /// <summary>
    /// Environment name for logging context
    /// </summary>
    public string Environment { get; set; } = "Development";

    /// <summary>
    /// Whether to include machine name in logs
    /// </summary>
    public bool IncludeMachineName { get; set; } = true;

    /// <summary>
    /// Whether to include process and thread IDs
    /// </summary>
    public bool IncludeProcessInfo { get; set; } = false;

    /// <summary>
    /// Whether to include scope information
    /// </summary>
    public bool IncludeScopes { get; set; } = true;

    /// <summary>
    /// Default properties to include in all log entries
    /// </summary>
    public Dictionary<string, string> DefaultProperties { get; set; } = new();

    /// <summary>
    /// Logging strategies configuration
    /// </summary>
    public LoggingStrategiesOptions Strategies { get; set; } = new();

    /// <summary>
    /// Console logging configuration
    /// </summary>
    public ConsoleLoggingOptions Console { get; set; } = new();

    /// <summary>
    /// Database logging configuration
    /// </summary>
    public DatabaseLoggingOptions Database { get; set; } = new();

    /// <summary>
    /// File logging configuration
    /// </summary>
    public FileLoggingOptions File { get; set; } = new();

    /// <summary>
    /// Validates the configuration
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ApplicationName))
            throw new ArgumentException("ApplicationName cannot be null or empty", nameof(ApplicationName));

        if (string.IsNullOrWhiteSpace(Environment))
            throw new ArgumentException("Environment cannot be null or empty", nameof(Environment));

        Strategies.Validate();
        Console.Validate();
        Database.Validate();
        File.Validate();
    }
}

/// <summary>
/// Configuration for logging strategies
/// </summary>
public class LoggingStrategiesOptions
{
    /// <summary>
    /// Enabled logging strategies
    /// </summary>
    public List<string> Enabled { get; set; } = new() { "Console" };

    /// <summary>
    /// Whether to enable async logging
    /// </summary>
    public bool EnableAsync { get; set; } = true;

    /// <summary>
    /// Buffer size for async logging
    /// </summary>
    public int AsyncBufferSize { get; set; } = 1000;

    /// <summary>
    /// Flush timeout for async logging
    /// </summary>
    public TimeSpan AsyncFlushTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Whether to enable batch processing
    /// </summary>
    public bool EnableBatching { get; set; } = true;

    /// <summary>
    /// Batch size for processing
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Batch timeout
    /// </summary>
    public TimeSpan BatchTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Validates the configuration
    /// </summary>
    public void Validate()
    {
        if (AsyncBufferSize <= 0)
            throw new ArgumentException("AsyncBufferSize must be greater than 0", nameof(AsyncBufferSize));

        if (BatchSize <= 0)
            throw new ArgumentException("BatchSize must be greater than 0", nameof(BatchSize));

        if (AsyncFlushTimeout <= TimeSpan.Zero)
            throw new ArgumentException("AsyncFlushTimeout must be greater than zero", nameof(AsyncFlushTimeout));

        if (BatchTimeout <= TimeSpan.Zero)
            throw new ArgumentException("BatchTimeout must be greater than zero", nameof(BatchTimeout));
    }
}

/// <summary>
/// Console logging configuration
/// </summary>
public class ConsoleLoggingOptions
{
    /// <summary>
    /// Whether console logging is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Minimum level for console logging
    /// </summary>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Whether to use colored output
    /// </summary>
    public bool UseColors { get; set; } = true;

    /// <summary>
    /// Output template for console messages
    /// </summary>
    public string OutputTemplate { get; set; } = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";

    /// <summary>
    /// Whether to include thread information
    /// </summary>
    public bool IncludeThreadInfo { get; set; } = false;

    /// <summary>
    /// Validates the configuration
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(OutputTemplate))
            throw new ArgumentException("OutputTemplate cannot be null or empty", nameof(OutputTemplate));
    }
}

/// <summary>
/// Database logging configuration
/// </summary>
public class DatabaseLoggingOptions
{
    /// <summary>
    /// Whether database logging is enabled
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Minimum level for database logging
    /// </summary>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Warning;

    /// <summary>
    /// Database connection string
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Database table name for logs
    /// </summary>
    public string TableName { get; set; } = "Logs";

    /// <summary>
    /// Database schema name
    /// </summary>
    public string? SchemaName { get; set; }

    /// <summary>
    /// Whether to auto-create the log table
    /// </summary>
    public bool AutoCreateSqlTable { get; set; } = true;

    /// <summary>
    /// Batch size for database inserts
    /// </summary>
    public int BatchSize { get; set; } = 50;

    /// <summary>
    /// Batch timeout for database inserts
    /// </summary>
    public TimeSpan BatchTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Connection timeout for database operations
    /// </summary>
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Validates the configuration
    /// </summary>
    public void Validate()
    {
        if (Enabled && string.IsNullOrWhiteSpace(ConnectionString))
            throw new ArgumentException("ConnectionString is required when database logging is enabled", nameof(ConnectionString));

        if (string.IsNullOrWhiteSpace(TableName))
            throw new ArgumentException("TableName cannot be null or empty", nameof(TableName));

        if (BatchSize <= 0)
            throw new ArgumentException("BatchSize must be greater than 0", nameof(BatchSize));

        if (BatchTimeout <= TimeSpan.Zero)
            throw new ArgumentException("BatchTimeout must be greater than zero", nameof(BatchTimeout));

        if (ConnectionTimeout <= TimeSpan.Zero)
            throw new ArgumentException("ConnectionTimeout must be greater than zero", nameof(ConnectionTimeout));
    }
}

/// <summary>
/// File logging configuration
/// </summary>
public class FileLoggingOptions
{
    /// <summary>
    /// Whether file logging is enabled
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Minimum level for file logging
    /// </summary>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// File path template
    /// </summary>
    public string Path { get; set; } = "logs/log-.txt";

    /// <summary>
    /// Rolling interval for log files
    /// </summary>
    public RollingInterval RollingInterval { get; set; } = RollingInterval.Day;

    /// <summary>
    /// Maximum file size in bytes
    /// </summary>
    public long? FileSizeLimitBytes { get; set; } = 100 * 1024 * 1024; // 100MB

    /// <summary>
    /// Number of log files to retain
    /// </summary>
    public int? RetainedFileCountLimit { get; set; } = 31;

    /// <summary>
    /// Output template for file messages
    /// </summary>
    public string OutputTemplate { get; set; } = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

    /// <summary>
    /// Validates the configuration
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Path))
            throw new ArgumentException("Path cannot be null or empty", nameof(Path));

        if (string.IsNullOrWhiteSpace(OutputTemplate))
            throw new ArgumentException("OutputTemplate cannot be null or empty", nameof(OutputTemplate));

        if (FileSizeLimitBytes.HasValue && FileSizeLimitBytes <= 0)
            throw new ArgumentException("FileSizeLimitBytes must be greater than 0", nameof(FileSizeLimitBytes));

        if (RetainedFileCountLimit.HasValue && RetainedFileCountLimit <= 0)
            throw new ArgumentException("RetainedFileCountLimit must be greater than 0", nameof(RetainedFileCountLimit));
    }
}

/// <summary>
/// Rolling interval for log files
/// </summary>
public enum RollingInterval
{
    Infinite,
    Year,
    Month,
    Day,
    Hour,
    Minute
}
