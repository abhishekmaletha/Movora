using Core.Logging.Abstractions;
using Core.Logging.Configuration;
using Core.Logging.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Logging.Strategies;

/// <summary>
/// Database logging strategy implementation
/// </summary>
public class DatabaseLoggingStrategy : ILoggingStrategy
{
    private readonly DatabaseLoggingOptions _options;
    private readonly IDbContextFactory<LoggingDbContext> _dbContextFactory;

    public DatabaseLoggingStrategy(
        IOptions<CoreLoggingOptions> options,
        IDbContextFactory<LoggingDbContext> dbContextFactory)
    {
        _options = options.Value.Database;
        _dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc />
    public string Name => "Database";

    /// <inheritdoc />
    public bool IsEnabled => _options.Enabled && !string.IsNullOrEmpty(_options.ConnectionString);

    /// <inheritdoc />
    public async Task WriteLogAsync(LogEntry logEntry, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || !logEntry.Level.IsEnabled(_options.MinimumLevel))
            return;

        try
        {
            using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            
            var dbLogEntry = MapToDbLogEntry(logEntry);
            context.Logs.Add(dbLogEntry);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Ignore database write errors to prevent logging from causing application failures
        }
    }

    /// <inheritdoc />
    public async Task WriteLogsAsync(IEnumerable<LogEntry> logEntries, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
            return;

        var filteredEntries = logEntries
            .Where(entry => entry.Level.IsEnabled(_options.MinimumLevel))
            .ToList();

        if (!filteredEntries.Any())
            return;

        try
        {
            using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            
            var dbLogEntries = filteredEntries.Select(MapToDbLogEntry);
            context.Logs.AddRange(dbLogEntries);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Ignore database write errors to prevent logging from causing application failures
        }
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        if (!IsEnabled)
            return;

        try
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            
            if (_options.AutoCreateSqlTable)
            {
                await context.Database.EnsureCreatedAsync();
            }
        }
        catch
        {
            // Ignore initialization errors
        }
    }

    /// <inheritdoc />
    public Task DisposeAsync()
    {
        // No cleanup needed for database
        return Task.CompletedTask;
    }

    private static DbLogEntry MapToDbLogEntry(LogEntry logEntry)
    {
        return new DbLogEntry
        {
            Id = logEntry.Id,
            Timestamp = logEntry.Timestamp,
            Level = logEntry.Level.ToString(),
            Message = logEntry.Message,
            MessageTemplate = logEntry.MessageTemplate,
            Exception = logEntry.Exception,
            Category = logEntry.Category,
            Application = logEntry.Application,
            Environment = logEntry.Environment,
            MachineName = logEntry.MachineName,
            UserId = logEntry.UserId,
            CorrelationId = logEntry.CorrelationId,
            Properties = logEntry.Properties,
            Scope = logEntry.Scope,
            ThreadId = logEntry.ThreadId,
            ProcessId = logEntry.ProcessId
        };
    }
}

/// <summary>
/// Entity Framework DbContext for logging
/// </summary>
public class LoggingDbContext : DbContext
{
    private readonly DatabaseLoggingOptions _options;

    public LoggingDbContext(DbContextOptions<LoggingDbContext> options, IOptions<CoreLoggingOptions> loggingOptions)
        : base(options)
    {
        _options = loggingOptions.Value.Database;
    }

    public DbSet<DbLogEntry> Logs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var entity = modelBuilder.Entity<DbLogEntry>();
        
        // Configure table name and schema
        var tableName = _options.TableName;
        if (!string.IsNullOrEmpty(_options.SchemaName))
        {
            entity.ToTable(tableName, _options.SchemaName);
        }
        else
        {
            entity.ToTable(tableName);
        }

        // Configure indexes for performance
        entity.HasIndex(e => e.Timestamp);
        entity.HasIndex(e => e.Level);
        entity.HasIndex(e => e.Category);
        entity.HasIndex(e => e.CorrelationId);
        entity.HasIndex(e => new { e.Application, e.Environment });

        // Configure property lengths
        entity.Property(e => e.Level).HasMaxLength(20);
        entity.Property(e => e.Category).HasMaxLength(200);
        entity.Property(e => e.Application).HasMaxLength(100);
        entity.Property(e => e.Environment).HasMaxLength(50);
        entity.Property(e => e.MachineName).HasMaxLength(100);
        entity.Property(e => e.UserId).HasMaxLength(100);
        entity.Property(e => e.CorrelationId).HasMaxLength(100);
        entity.Property(e => e.Scope).HasMaxLength(500);
    }
}

/// <summary>
/// Database entity for log entries
/// </summary>
[Table("Logs")]
public class DbLogEntry
{
    [Key]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }

    [MaxLength(20)]
    public string Level { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? MessageTemplate { get; set; }

    public string? Exception { get; set; }

    [MaxLength(200)]
    public string? Category { get; set; }

    [MaxLength(100)]
    public string? Application { get; set; }

    [MaxLength(50)]
    public string? Environment { get; set; }

    [MaxLength(100)]
    public string? MachineName { get; set; }

    [MaxLength(100)]
    public string? UserId { get; set; }

    [MaxLength(100)]
    public string? CorrelationId { get; set; }

    public string? Properties { get; set; }

    [MaxLength(500)]
    public string? Scope { get; set; }

    public int? ThreadId { get; set; }

    public int? ProcessId { get; set; }
}
