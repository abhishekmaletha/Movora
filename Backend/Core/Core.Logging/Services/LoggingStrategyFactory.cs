using Core.Logging.Abstractions;
using Core.Logging.Configuration;
using Core.Logging.Strategies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Core.Logging.Services;

/// <summary>
/// Factory for creating and managing logging strategies
/// </summary>
public class LoggingStrategyFactory : ILoggingStrategyFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly CoreLoggingOptions _options;
    private readonly Dictionary<string, ILoggingStrategy> _strategies;
    private readonly object _lock = new();

    public LoggingStrategyFactory(IServiceProvider serviceProvider, IOptions<CoreLoggingOptions> options)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _strategies = new Dictionary<string, ILoggingStrategy>(StringComparer.OrdinalIgnoreCase);
        
        InitializeStrategies();
    }

    /// <inheritdoc />
    public ILoggingStrategy CreateStrategy(string strategyName)
    {
        lock (_lock)
        {
            if (_strategies.TryGetValue(strategyName, out var existingStrategy))
            {
                return existingStrategy;
            }

            var strategy = CreateStrategyInternal(strategyName);
            if (strategy != null)
            {
                _strategies[strategyName] = strategy;
                // Initialize the strategy asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await strategy.InitializeAsync();
                    }
                    catch
                    {
                        // Ignore initialization errors
                    }
                });
            }

            return strategy ?? new NullLoggingStrategy();
        }
    }

    /// <inheritdoc />
    public IEnumerable<ILoggingStrategy> GetAllStrategies()
    {
        lock (_lock)
        {
            return _strategies.Values.ToList();
        }
    }

    /// <inheritdoc />
    public IEnumerable<ILoggingStrategy> GetEnabledStrategies()
    {
        lock (_lock)
        {
            return _strategies.Values.Where(s => s.IsEnabled).ToList();
        }
    }

    private void InitializeStrategies()
    {
        foreach (var strategyName in _options.Strategies.Enabled)
        {
            CreateStrategy(strategyName);
        }
    }

    private ILoggingStrategy? CreateStrategyInternal(string strategyName)
    {
        return strategyName.ToLowerInvariant() switch
        {
            "console" => _serviceProvider.GetService<ConsoleLoggingStrategy>(),
            "database" => _serviceProvider.GetService<DatabaseLoggingStrategy>(),
            "file" => _serviceProvider.GetService<FileLoggingStrategy>(),
            _ => null
        };
    }
}

/// <summary>
/// Null object pattern implementation for logging strategy
/// </summary>
public class NullLoggingStrategy : ILoggingStrategy
{
    /// <inheritdoc />
    public string Name => "Null";

    /// <inheritdoc />
    public bool IsEnabled => false;

    /// <inheritdoc />
    public Task WriteLogAsync(Models.LogEntry logEntry, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task WriteLogsAsync(IEnumerable<Models.LogEntry> logEntries, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
