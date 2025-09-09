namespace Core.Logging.Abstractions;

/// <summary>
/// Main application logger interface with simplified logging methods
/// </summary>
public interface IAppLogger
{
    /// <summary>
    /// Logs an informational message
    /// </summary>
    /// <param name="message">The message to log</param>
    /// <param name="context">Optional context information</param>
    void LogInformation(string message, object? context = null);

    /// <summary>
    /// Logs an informational message with parameters
    /// </summary>
    /// <param name="messageTemplate">The message template</param>
    /// <param name="args">Arguments for the template</param>
    void LogInformation(string messageTemplate, params object[] args);

    /// <summary>
    /// Logs a warning message
    /// </summary>
    /// <param name="message">The message to log</param>
    /// <param name="context">Optional context information</param>
    void LogWarning(string message, object? context = null);

    /// <summary>
    /// Logs a warning message with parameters
    /// </summary>
    /// <param name="messageTemplate">The message template</param>
    /// <param name="args">Arguments for the template</param>
    void LogWarning(string messageTemplate, params object[] args);

    /// <summary>
    /// Logs an error message
    /// </summary>
    /// <param name="message">The message to log</param>
    /// <param name="exception">Optional exception</param>
    /// <param name="context">Optional context information</param>
    void LogError(string message, Exception? exception = null, object? context = null);

    /// <summary>
    /// Logs an error message with parameters
    /// </summary>
    /// <param name="exception">The exception to log</param>
    /// <param name="messageTemplate">The message template</param>
    /// <param name="args">Arguments for the template</param>
    void LogError(Exception exception, string messageTemplate, params object[] args);

    /// <summary>
    /// Logs a debug message
    /// </summary>
    /// <param name="message">The message to log</param>
    /// <param name="context">Optional context information</param>
    void LogDebug(string message, object? context = null);

    /// <summary>
    /// Logs a debug message with parameters
    /// </summary>
    /// <param name="messageTemplate">The message template</param>
    /// <param name="args">Arguments for the template</param>
    void LogDebug(string messageTemplate, params object[] args);

    /// <summary>
    /// Logs a critical/fatal message
    /// </summary>
    /// <param name="message">The message to log</param>
    /// <param name="exception">Optional exception</param>
    /// <param name="context">Optional context information</param>
    void LogCritical(string message, Exception? exception = null, object? context = null);

    /// <summary>
    /// Logs a critical/fatal message with parameters
    /// </summary>
    /// <param name="exception">The exception to log</param>
    /// <param name="messageTemplate">The message template</param>
    /// <param name="args">Arguments for the template</param>
    void LogCritical(Exception exception, string messageTemplate, params object[] args);

    /// <summary>
    /// Begins a logging scope
    /// </summary>
    /// <param name="state">The scope state</param>
    /// <returns>Disposable scope</returns>
    IDisposable BeginScope<TState>(TState state) where TState : notnull;

    /// <summary>
    /// Begins a logging scope with a string
    /// </summary>
    /// <param name="message">The scope message</param>
    /// <returns>Disposable scope</returns>
    IDisposable BeginScope(string message);
}
