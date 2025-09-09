using Core.Logging.Abstractions;
using Core.Logging.Configuration;
using Core.Logging.Models;
using Microsoft.Extensions.Options;

namespace Core.Logging.Strategies;

/// <summary>
/// Console logging strategy implementation
/// </summary>
public class ConsoleLoggingStrategy : ILoggingStrategy
{
    private readonly ConsoleLoggingOptions _options;
    private readonly object _lock = new();

    public ConsoleLoggingStrategy(IOptions<CoreLoggingOptions> options)
    {
        _options = options.Value.Console;
    }

    /// <inheritdoc />
    public string Name => "Console";

    /// <inheritdoc />
    public bool IsEnabled => _options.Enabled;

    /// <inheritdoc />
    public Task WriteLogAsync(LogEntry logEntry, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || !logEntry.Level.IsEnabled(_options.MinimumLevel))
            return Task.CompletedTask;

        return Task.Run(() => WriteToConsole(logEntry), cancellationToken);
    }

    /// <inheritdoc />
    public Task WriteLogsAsync(IEnumerable<LogEntry> logEntries, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
            return Task.CompletedTask;

        var filteredEntries = logEntries.Where(entry => entry.Level.IsEnabled(_options.MinimumLevel));

        return Task.Run(() =>
        {
            foreach (var logEntry in filteredEntries)
            {
                WriteToConsole(logEntry);
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task InitializeAsync()
    {
        // No initialization needed for console
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DisposeAsync()
    {
        // No cleanup needed for console
        return Task.CompletedTask;
    }

    private void WriteToConsole(LogEntry logEntry)
    {
        lock (_lock)
        {
            try
            {
                var formattedMessage = FormatMessage(logEntry);
                
                if (_options.UseColors)
                {
                    var color = GetLogLevelColor(logEntry.Level);
                    Console.ForegroundColor = color;
                }

                Console.WriteLine(formattedMessage);

                if (_options.UseColors)
                {
                    Console.ResetColor();
                }
            }
            catch
            {
                // Ignore console write errors
            }
        }
    }

    private string FormatMessage(LogEntry logEntry)
    {
        var template = _options.OutputTemplate;
        
        // Replace common placeholders
        var message = template
            .Replace("{Timestamp:HH:mm:ss}", logEntry.Timestamp.ToString("HH:mm:ss"))
            .Replace("{Timestamp:yyyy-MM-dd HH:mm:ss}", logEntry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"))
            .Replace("{Timestamp:yyyy-MM-dd HH:mm:ss.fff}", logEntry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"))
            .Replace("{Level:u3}", logEntry.Level.ToShortString())
            .Replace("{Level}", logEntry.Level.ToString())
            .Replace("{Message:lj}", logEntry.Message)
            .Replace("{Message}", logEntry.Message)
            .Replace("{NewLine}", Environment.NewLine);

        // Add exception if present
        if (!string.IsNullOrEmpty(logEntry.Exception))
        {
            message = message.Replace("{Exception}", Environment.NewLine + logEntry.Exception);
        }
        else
        {
            message = message.Replace("{Exception}", string.Empty);
        }

        // Add category if present and template supports it
        if (!string.IsNullOrEmpty(logEntry.Category))
        {
            message = message.Replace("{Category}", logEntry.Category);
        }

        // Add thread info if enabled
        if (_options.IncludeThreadInfo && logEntry.ThreadId.HasValue)
        {
            message = $"[Thread:{logEntry.ThreadId}] {message}";
        }

        // Add correlation ID if present
        if (!string.IsNullOrEmpty(logEntry.CorrelationId))
        {
            message = $"[{logEntry.CorrelationId}] {message}";
        }

        return message;
    }

    private static ConsoleColor GetLogLevelColor(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => ConsoleColor.Gray,
            LogLevel.Debug => ConsoleColor.Gray,
            LogLevel.Information => ConsoleColor.White,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            LogLevel.Critical => ConsoleColor.Magenta,
            _ => ConsoleColor.White
        };
    }
}
