using System.Text.Json;

namespace Core.Logging.Models;

/// <summary>
/// Represents a log entry
/// </summary>
public class LogEntry
{
    /// <summary>
    /// Unique identifier for the log entry
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Timestamp when the log entry was created
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Log level
    /// </summary>
    public LogLevel Level { get; set; }

    /// <summary>
    /// Log message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Message template (if using structured logging)
    /// </summary>
    public string? MessageTemplate { get; set; }

    /// <summary>
    /// Exception information (if any)
    /// </summary>
    public string? Exception { get; set; }

    /// <summary>
    /// Source category (typically the class name)
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Application name
    /// </summary>
    public string? Application { get; set; }

    /// <summary>
    /// Environment (Development, Staging, Production)
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Machine name where the log was generated
    /// </summary>
    public string? MachineName { get; set; }

    /// <summary>
    /// User ID (if available)
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Correlation ID for request tracking
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Additional properties as JSON
    /// </summary>
    public string? Properties { get; set; }

    /// <summary>
    /// Scope information
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// Thread ID
    /// </summary>
    public int? ThreadId { get; set; }

    /// <summary>
    /// Process ID
    /// </summary>
    public int? ProcessId { get; set; }

    /// <summary>
    /// Creates a LogEntry from an exception
    /// </summary>
    /// <param name="exception">The exception</param>
    /// <param name="message">Optional message</param>
    /// <param name="level">Log level (default: Error)</param>
    /// <returns>LogEntry instance</returns>
    public static LogEntry FromException(Exception exception, string? message = null, LogLevel level = LogLevel.Error)
    {
        return new LogEntry
        {
            Level = level,
            Message = message ?? exception.Message,
            Exception = JsonSerializer.Serialize(new
            {
                Type = exception.GetType().FullName,
                Message = exception.Message,
                StackTrace = exception.StackTrace,
                InnerException = exception.InnerException?.Message,
                Data = exception.Data.Count > 0 ? exception.Data : null
            }),
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Adds properties to the log entry
    /// </summary>
    /// <param name="properties">Properties to add</param>
    public void AddProperties(object properties)
    {
        if (properties != null)
        {
            Properties = JsonSerializer.Serialize(properties);
        }
    }

    /// <summary>
    /// Gets properties as a dictionary
    /// </summary>
    /// <returns>Properties dictionary</returns>
    public Dictionary<string, object>? GetProperties()
    {
        if (string.IsNullOrEmpty(Properties))
            return null;

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(Properties);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Creates a simple informational log entry
    /// </summary>
    /// <param name="message">The message</param>
    /// <param name="category">Optional category</param>
    /// <returns>LogEntry instance</returns>
    public static LogEntry Information(string message, string? category = null)
    {
        return new LogEntry
        {
            Level = LogLevel.Information,
            Message = message,
            Category = category,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a warning log entry
    /// </summary>
    /// <param name="message">The message</param>
    /// <param name="category">Optional category</param>
    /// <returns>LogEntry instance</returns>
    public static LogEntry Warning(string message, string? category = null)
    {
        return new LogEntry
        {
            Level = LogLevel.Warning,
            Message = message,
            Category = category,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates an error log entry
    /// </summary>
    /// <param name="message">The message</param>
    /// <param name="exception">Optional exception</param>
    /// <param name="category">Optional category</param>
    /// <returns>LogEntry instance</returns>
    public static LogEntry Error(string message, Exception? exception = null, string? category = null)
    {
        var entry = new LogEntry
        {
            Level = LogLevel.Error,
            Message = message,
            Category = category,
            Timestamp = DateTime.UtcNow
        };

        if (exception != null)
        {
            entry.Exception = JsonSerializer.Serialize(new
            {
                Type = exception.GetType().FullName,
                Message = exception.Message,
                StackTrace = exception.StackTrace,
                InnerException = exception.InnerException?.Message
            });
        }

        return entry;
    }
}
