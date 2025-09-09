namespace Core.Logging.Models;

/// <summary>
/// Defines the logging severity levels
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Logs that contain the most detailed messages. These messages may contain sensitive application data.
    /// These messages are disabled by default and should never be enabled in a production environment.
    /// </summary>
    Trace = 0,

    /// <summary>
    /// Logs that are used for interactive investigation during development.  These logs should primarily contain
    /// information useful for debugging and have no long-term value.
    /// </summary>
    Debug = 1,

    /// <summary>
    /// Logs that track the general flow of the application. These logs should have long-term value.
    /// </summary>
    Information = 2,

    /// <summary>
    /// Logs that highlight an abnormal or unexpected event in the application flow, but do not otherwise cause the
    /// application execution to stop.
    /// </summary>
    Warning = 3,

    /// <summary>
    /// Logs that highlight when the current flow of execution is stopped due to a failure. These should indicate a
    /// failure in the current activity, not an application-wide failure.
    /// </summary>
    Error = 4,

    /// <summary>
    /// Logs that describe an unrecoverable application or system crash, or a catastrophic failure that requires
    /// immediate attention.
    /// </summary>
    Critical = 5,

    /// <summary>
    /// Not used for writing log messages. Specifies that a logging category should not write any messages.
    /// </summary>
    None = 6
}

/// <summary>
/// Extension methods for LogLevel
/// </summary>
public static class LogLevelExtensions
{
    /// <summary>
    /// Gets the string representation of the log level
    /// </summary>
    /// <param name="logLevel">The log level</param>
    /// <returns>String representation</returns>
    public static string ToString(this LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "TRACE",
            LogLevel.Debug => "DEBUG",
            LogLevel.Information => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "ERROR",
            LogLevel.Critical => "FATAL",
            LogLevel.None => "NONE",
            _ => "UNKNOWN"
        };
    }

    /// <summary>
    /// Gets the short string representation of the log level
    /// </summary>
    /// <param name="logLevel">The log level</param>
    /// <returns>Short string representation</returns>
    public static string ToShortString(this LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "TRC",
            LogLevel.Debug => "DBG",
            LogLevel.Information => "INF",
            LogLevel.Warning => "WRN",
            LogLevel.Error => "ERR",
            LogLevel.Critical => "FTL",
            LogLevel.None => "NON",
            _ => "UNK"
        };
    }

    /// <summary>
    /// Parses a string to LogLevel
    /// </summary>
    /// <param name="logLevel">The string representation</param>
    /// <returns>LogLevel enum value</returns>
    public static LogLevel ParseLogLevel(string logLevel)
    {
        return logLevel.ToUpperInvariant() switch
        {
            "TRACE" or "TRC" => LogLevel.Trace,
            "DEBUG" or "DBG" => LogLevel.Debug,
            "INFORMATION" or "INFO" or "INF" => LogLevel.Information,
            "WARNING" or "WARN" or "WRN" => LogLevel.Warning,
            "ERROR" or "ERR" => LogLevel.Error,
            "CRITICAL" or "FATAL" or "FTL" => LogLevel.Critical,
            "NONE" or "NON" => LogLevel.None,
            _ => LogLevel.Information
        };
    }

    /// <summary>
    /// Checks if the log level is enabled for the given minimum level
    /// </summary>
    /// <param name="logLevel">The log level to check</param>
    /// <param name="minimumLevel">The minimum level required</param>
    /// <returns>True if enabled</returns>
    public static bool IsEnabled(this LogLevel logLevel, LogLevel minimumLevel)
    {
        return logLevel >= minimumLevel && logLevel != LogLevel.None;
    }
}
