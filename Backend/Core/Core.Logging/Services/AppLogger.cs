using Core.Logging.Abstractions;
using Core.Logging.Configuration;
using Core.Logging.Models;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Core.Logging.Services;

/// <summary>
/// Main application logger implementation
/// </summary>
public class AppLogger : IAppLogger
{
    private readonly CoreLoggingOptions _options;
    private readonly ILoggingStrategyFactory _strategyFactory;
    private readonly string? _categoryName;
    private readonly Stack<string> _scopeStack = new();
    private readonly object _lock = new();

    public AppLogger(
        IOptions<CoreLoggingOptions> options,
        ILoggingStrategyFactory strategyFactory,
        string? categoryName = null)
    {
        _options = options.Value;
        _strategyFactory = strategyFactory;
        _categoryName = categoryName;
    }

    /// <inheritdoc />
    public void LogInformation(string message, object? context = null)
    {
        LogInternal(LogLevel.Information, message, null, context);
    }

    /// <inheritdoc />
    public void LogInformation(string messageTemplate, params object[] args)
    {
        LogInternal(LogLevel.Information, string.Format(messageTemplate, args), messageTemplate);
    }

    /// <inheritdoc />
    public void LogWarning(string message, object? context = null)
    {
        LogInternal(LogLevel.Warning, message, null, context);
    }

    /// <inheritdoc />
    public void LogWarning(string messageTemplate, params object[] args)
    {
        LogInternal(LogLevel.Warning, string.Format(messageTemplate, args), messageTemplate);
    }

    /// <inheritdoc />
    public void LogError(string message, Exception? exception = null, object? context = null)
    {
        LogInternal(LogLevel.Error, message, null, context, exception);
    }

    /// <inheritdoc />
    public void LogError(Exception exception, string messageTemplate, params object[] args)
    {
        LogInternal(LogLevel.Error, string.Format(messageTemplate, args), messageTemplate, null, exception);
    }

    /// <inheritdoc />
    public void LogDebug(string message, object? context = null)
    {
        LogInternal(LogLevel.Debug, message, null, context);
    }

    /// <inheritdoc />
    public void LogDebug(string messageTemplate, params object[] args)
    {
        LogInternal(LogLevel.Debug, string.Format(messageTemplate, args), messageTemplate);
    }

    /// <inheritdoc />
    public void LogCritical(string message, Exception? exception = null, object? context = null)
    {
        LogInternal(LogLevel.Critical, message, null, context, exception);
    }

    /// <inheritdoc />
    public void LogCritical(Exception exception, string messageTemplate, params object[] args)
    {
        LogInternal(LogLevel.Critical, string.Format(messageTemplate, args), messageTemplate, null, exception);
    }

    /// <inheritdoc />
    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        return BeginScope(state.ToString() ?? string.Empty);
    }

    /// <inheritdoc />
    public IDisposable BeginScope(string message)
    {
        lock (_lock)
        {
            _scopeStack.Push(message);
        }
        return new ScopeDisposable(this);
    }

    internal void EndScope()
    {
        lock (_lock)
        {
            if (_scopeStack.Count > 0)
            {
                _scopeStack.Pop();
            }
        }
    }

    private void LogInternal(LogLevel level, string message, string? messageTemplate = null, object? context = null, Exception? exception = null)
    {
        if (!level.IsEnabled(_options.MinimumLevel))
            return;

        var logEntry = CreateLogEntry(level, message, messageTemplate, context, exception);

        // Write to all enabled strategies asynchronously
        var strategies = _strategyFactory.GetEnabledStrategies();
        foreach (var strategy in strategies)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await strategy.WriteLogAsync(logEntry);
                }
                catch
                {
                    // Ignore strategy errors to prevent logging from causing application failures
                }
            });
        }
    }

    private LogEntry CreateLogEntry(LogLevel level, string message, string? messageTemplate = null, object? context = null, Exception? exception = null)
    {
        var logEntry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = level,
            Message = message,
            MessageTemplate = messageTemplate,
            Category = _categoryName,
            Application = _options.ApplicationName,
            Environment = _options.Environment
        };

        // Add machine name if configured
        if (_options.IncludeMachineName)
        {
            logEntry.MachineName = System.Environment.MachineName;
        }

        // Add process info if configured
        if (_options.IncludeProcessInfo)
        {
            logEntry.ThreadId = Thread.CurrentThread.ManagedThreadId;
            logEntry.ProcessId = Process.GetCurrentProcess().Id;
        }

        // Add scope information if configured
        if (_options.IncludeScopes)
        {
            lock (_lock)
            {
                if (_scopeStack.Count > 0)
                {
                    logEntry.Scope = string.Join(" => ", _scopeStack.Reverse());
                }
            }
        }

        // Add exception information
        if (exception != null)
        {
            logEntry.Exception = System.Text.Json.JsonSerializer.Serialize(new
            {
                Type = exception.GetType().FullName,
                Message = exception.Message,
                StackTrace = exception.StackTrace,
                InnerException = exception.InnerException?.Message,
                Data = exception.Data.Count > 0 ? exception.Data : null
            });
        }

        // Add context properties
        if (context != null)
        {
            logEntry.AddProperties(context);
        }

        // Add default properties
        if (_options.DefaultProperties.Any())
        {
            var allProperties = new Dictionary<string, object>();
            
            // Add default properties
            foreach (var prop in _options.DefaultProperties)
            {
                allProperties[prop.Key] = prop.Value;
            }

            // Add context properties (will override defaults if same key)
            var contextProps = logEntry.GetProperties();
            if (contextProps != null)
            {
                foreach (var prop in contextProps)
                {
                    allProperties[prop.Key] = prop.Value;
                }
            }

            logEntry.AddProperties(allProperties);
        }

        return logEntry;
    }

    private class ScopeDisposable : IDisposable
    {
        private readonly AppLogger _logger;
        private bool _disposed;

        public ScopeDisposable(AppLogger logger)
        {
            _logger = logger;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _logger.EndScope();
                _disposed = true;
            }
        }
    }
}

/// <summary>
/// Generic typed logger implementation
/// </summary>
/// <typeparam name="T">The type associated with the logger</typeparam>
public class AppLogger<T> : IAppLogger<T>
{
    private readonly IAppLogger _logger;

    public AppLogger(IOptions<CoreLoggingOptions> options, ILoggingStrategyFactory strategyFactory)
    {
        _logger = new AppLogger(options, strategyFactory, typeof(T).FullName);
    }

    /// <inheritdoc />
    public void LogInformation(string message, object? context = null) => _logger.LogInformation(message, context);

    /// <inheritdoc />
    public void LogInformation(string messageTemplate, params object[] args) => _logger.LogInformation(messageTemplate, args);

    /// <inheritdoc />
    public void LogWarning(string message, object? context = null) => _logger.LogWarning(message, context);

    /// <inheritdoc />
    public void LogWarning(string messageTemplate, params object[] args) => _logger.LogWarning(messageTemplate, args);

    /// <inheritdoc />
    public void LogError(string message, Exception? exception = null, object? context = null) => _logger.LogError(message, exception, context);

    /// <inheritdoc />
    public void LogError(Exception exception, string messageTemplate, params object[] args) => _logger.LogError(exception, messageTemplate, args);

    /// <inheritdoc />
    public void LogDebug(string message, object? context = null) => _logger.LogDebug(message, context);

    /// <inheritdoc />
    public void LogDebug(string messageTemplate, params object[] args) => _logger.LogDebug(messageTemplate, args);

    /// <inheritdoc />
    public void LogCritical(string message, Exception? exception = null, object? context = null) => _logger.LogCritical(message, exception, context);

    /// <inheritdoc />
    public void LogCritical(Exception exception, string messageTemplate, params object[] args) => _logger.LogCritical(exception, messageTemplate, args);

    /// <inheritdoc />
    public IDisposable BeginScope<TState>(TState state) where TState : notnull => _logger.BeginScope(state);

    /// <inheritdoc />
    public IDisposable BeginScope(string message) => _logger.BeginScope(message);
}
