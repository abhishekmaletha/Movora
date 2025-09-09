using Core.Logging.Abstractions;
using Core.Logging.Configuration;
using Core.Logging.Models;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text;

namespace Core.Logging.Strategies;

/// <summary>
/// File logging strategy implementation
/// </summary>
public class FileLoggingStrategy : ILoggingStrategy, IDisposable
{
    private readonly FileLoggingOptions _options;
    private readonly ConcurrentQueue<LogEntry> _logQueue;
    private readonly Timer _flushTimer;
    private readonly object _fileLock = new();
    private readonly string _currentLogFile;
    private DateTime _lastRollDate;
    private bool _disposed;

    public FileLoggingStrategy(IOptions<CoreLoggingOptions> options)
    {
        _options = options.Value.File;
        _logQueue = new ConcurrentQueue<LogEntry>();
        
        _currentLogFile = GetCurrentLogFileName();
        _lastRollDate = DateTime.Today;
        
        // Create directory if it doesn't exist
        var directory = Path.GetDirectoryName(_currentLogFile);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Start flush timer
        _flushTimer = new Timer(FlushLogs, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
    }

    /// <inheritdoc />
    public string Name => "File";

    /// <inheritdoc />
    public bool IsEnabled => _options.Enabled && !string.IsNullOrEmpty(_options.Path);

    /// <inheritdoc />
    public Task WriteLogAsync(LogEntry logEntry, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || !logEntry.Level.IsEnabled(_options.MinimumLevel))
            return Task.CompletedTask;

        _logQueue.Enqueue(logEntry);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task WriteLogsAsync(IEnumerable<LogEntry> logEntries, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
            return Task.CompletedTask;

        var filteredEntries = logEntries.Where(entry => entry.Level.IsEnabled(_options.MinimumLevel));
        
        foreach (var logEntry in filteredEntries)
        {
            _logQueue.Enqueue(logEntry);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task InitializeAsync()
    {
        if (!IsEnabled)
            return Task.CompletedTask;

        try
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(_currentLogFile);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Clean up old log files if retention limit is set
            CleanupOldLogFiles();
        }
        catch
        {
            // Ignore initialization errors
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DisposeAsync()
    {
        Dispose();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        
        // Flush remaining logs
        FlushLogs(null);
        
        // Dispose timer
        _flushTimer?.Dispose();
    }

    private void FlushLogs(object? state)
    {
        if (_disposed || _logQueue.IsEmpty)
            return;

        var logsToWrite = new List<LogEntry>();
        
        // Dequeue all pending logs
        while (_logQueue.TryDequeue(out var logEntry))
        {
            logsToWrite.Add(logEntry);
        }

        if (!logsToWrite.Any())
            return;

        try
        {
            WriteLogsToFile(logsToWrite);
        }
        catch
        {
            // Re-queue logs if write failed
            foreach (var log in logsToWrite)
            {
                _logQueue.Enqueue(log);
            }
        }
    }

    private void WriteLogsToFile(List<LogEntry> logs)
    {
        lock (_fileLock)
        {
            var currentLogFile = GetCurrentLogFileName();
            
            // Check if we need to roll the file
            if (ShouldRollFile())
            {
                CleanupOldLogFiles();
            }

            // Write logs to file
            var logLines = logs.Select(FormatLogEntry);
            File.AppendAllLines(currentLogFile, logLines, Encoding.UTF8);
        }
    }

    private string FormatLogEntry(LogEntry logEntry)
    {
        var template = _options.OutputTemplate;
        
        // Replace common placeholders
        var message = template
            .Replace("{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}", logEntry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"))
            .Replace("{Timestamp:yyyy-MM-dd HH:mm:ss}", logEntry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"))
            .Replace("{Timestamp}", logEntry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"))
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

        // Add category if present
        if (!string.IsNullOrEmpty(logEntry.Category))
        {
            message = message.Replace("{Category}", logEntry.Category);
        }

        // Add correlation ID if present
        if (!string.IsNullOrEmpty(logEntry.CorrelationId))
        {
            message = $"[{logEntry.CorrelationId}] {message}";
        }

        return message;
    }

    private string GetCurrentLogFileName()
    {
        var path = _options.Path;
        var now = DateTime.Now;

        // Replace date placeholders based on rolling interval
        return _options.RollingInterval switch
        {
            RollingInterval.Year => path.Replace("-", $"-{now:yyyy}-"),
            RollingInterval.Month => path.Replace("-", $"-{now:yyyyMM}-"),
            RollingInterval.Day => path.Replace("-", $"-{now:yyyyMMdd}-"),
            RollingInterval.Hour => path.Replace("-", $"-{now:yyyyMMddHH}-"),
            RollingInterval.Minute => path.Replace("-", $"-{now:yyyyMMddHHmm}-"),
            _ => path.Replace("-", $"-{now:yyyyMMdd}-")
        };
    }

    private bool ShouldRollFile()
    {
        var currentFile = GetCurrentLogFileName();
        
        // Check if file exists and size limit
        if (_options.FileSizeLimitBytes.HasValue && File.Exists(currentFile))
        {
            var fileInfo = new FileInfo(currentFile);
            if (fileInfo.Length >= _options.FileSizeLimitBytes.Value)
            {
                return true;
            }
        }

        // Check rolling interval
        var today = DateTime.Today;
        if (_options.RollingInterval != RollingInterval.Infinite && _lastRollDate < today)
        {
            _lastRollDate = today;
            return true;
        }

        return false;
    }

    private void CleanupOldLogFiles()
    {
        if (!_options.RetainedFileCountLimit.HasValue)
            return;

        try
        {
            var directory = Path.GetDirectoryName(_currentLogFile);
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                return;

            var fileName = Path.GetFileNameWithoutExtension(_options.Path);
            var extension = Path.GetExtension(_options.Path);
            
            var logFiles = Directory.GetFiles(directory, $"{fileName}*{extension}")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTime)
                .ToList();

            if (logFiles.Count > _options.RetainedFileCountLimit.Value)
            {
                var filesToDelete = logFiles.Skip(_options.RetainedFileCountLimit.Value);
                foreach (var file in filesToDelete)
                {
                    try
                    {
                        file.Delete();
                    }
                    catch
                    {
                        // Ignore deletion errors
                    }
                }
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
