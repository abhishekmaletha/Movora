using Core.Logging.Models;

namespace Core.Logging.Abstractions;

/// <summary>
/// Interface for different logging strategies (console, database, file, etc.)
/// </summary>
public interface ILoggingStrategy
{
    /// <summary>
    /// The name of the logging strategy
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Whether this strategy is enabled
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Writes a log entry using this strategy
    /// </summary>
    /// <param name="logEntry">The log entry to write</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task WriteLogAsync(LogEntry logEntry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes multiple log entries using this strategy
    /// </summary>
    /// <param name="logEntries">The log entries to write</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task WriteLogsAsync(IEnumerable<LogEntry> logEntries, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initializes the logging strategy
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Performs cleanup when the strategy is disposed
    /// </summary>
    Task DisposeAsync();
}

/// <summary>
/// Generic typed logger interface
/// </summary>
/// <typeparam name="T">The type associated with the logger</typeparam>
public interface IAppLogger<out T> : IAppLogger
{
}

/// <summary>
/// Interface for logging strategy factory
/// </summary>
public interface ILoggingStrategyFactory
{
    /// <summary>
    /// Creates a logging strategy by name
    /// </summary>
    /// <param name="strategyName">The strategy name</param>
    /// <returns>The logging strategy instance</returns>
    ILoggingStrategy CreateStrategy(string strategyName);

    /// <summary>
    /// Gets all available strategies
    /// </summary>
    /// <returns>Collection of available strategies</returns>
    IEnumerable<ILoggingStrategy> GetAllStrategies();

    /// <summary>
    /// Gets enabled strategies
    /// </summary>
    /// <returns>Collection of enabled strategies</returns>
    IEnumerable<ILoggingStrategy> GetEnabledStrategies();
}
