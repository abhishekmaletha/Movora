using Core.Persistence.BulkOperations;
using Core.Persistence.Configuration;
using Core.Persistence.ConnectionFactory;
using Core.Persistence.DataStoreStrategy;
using Core.Persistence.Helpers;
using Core.Persistence.TypeMapping;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Core.Persistence.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to register Core.Persistence services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Core.Persistence services with PostgreSQL support using configuration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddCorePersistence(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind PostgreSQL settings
        var postgresSettings = new PostgreSqlSettings();
        configuration.GetSection(PostgreSqlSettings.SectionName).Bind(postgresSettings);
        services.Configure<PostgreSqlSettings>(configuration.GetSection(PostgreSqlSettings.SectionName));

        return AddCorePersistenceCore(services, postgresSettings);
    }

    /// <summary>
    /// Adds Core.Persistence services with PostgreSQL support using connection string
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="connectionString">PostgreSQL connection string</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddCorePersistencePostgreSQL(this IServiceCollection services, string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

        var settings = new PostgreSqlSettings
        {
            ConnectionString = connectionString
        };

        services.Configure<PostgreSqlSettings>(options =>
        {
            options.ConnectionString = connectionString;
        });

        return AddCorePersistenceCore(services, settings);
    }

    /// <summary>
    /// Adds Core.Persistence services with PostgreSQL support using settings configuration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configureSettings">Settings configuration action</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddCorePersistencePostgreSQL(this IServiceCollection services, Action<PostgreSqlSettings> configureSettings)
    {
        var settings = new PostgreSqlSettings();
        configureSettings(settings);

        services.Configure<PostgreSqlSettings>(configureSettings);

        return AddCorePersistenceCore(services, settings);
    }

    /// <summary>
    /// Adds minimal Core.Persistence services for testing or custom scenarios
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="connectionString">PostgreSQL connection string</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddCorePersistenceMinimal(this IServiceCollection services, string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

        // Register minimal services
        services.AddSingleton<IConnectionFactory<IDbConnection>>(provider =>
            new PostgreSqlConnectionFactory(connectionString));

        services.AddSingleton<IDbConnectionFactory>(provider =>
            new PostgreSqlConnectionFactory(connectionString));

        services.AddScoped<IDatabaseHelper, PostgreSqlHelper>();
        services.AddScoped<IPostgreSqlHelper, PostgreSqlHelper>();

        return services;
    }

    /// <summary>
    /// Adds bulk operations services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddBulkOperations(this IServiceCollection services)
    {
        services.AddScoped<PostgreSqlBulkOperations>();
        services.Configure<BulkOperationOptions>(options => { }); // Default configuration
        
        return services;
    }

    /// <summary>
    /// Adds bulk operations services with configuration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configureBulkOptions">Bulk options configuration action</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddBulkOperations(this IServiceCollection services, Action<BulkOperationOptions> configureBulkOptions)
    {
        services.AddScoped<PostgreSqlBulkOperations>();
        services.Configure(configureBulkOptions);
        
        return services;
    }

    /// <summary>
    /// Adds composite type management services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddCompositeTypes(this IServiceCollection services)
    {
        services.AddSingleton<CompositeTypeManager>();
        return services;
    }

    /// <summary>
    /// Registers a composite type mapping
    /// </summary>
    /// <typeparam name="T">CLR type</typeparam>
    /// <param name="services">Service collection</param>
    /// <param name="typeName">PostgreSQL composite type name</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection RegisterCompositeType<T>(this IServiceCollection services, string typeName)
    {
        services.AddSingleton<ICompositeTypeRegistration>(new CompositeTypeRegistration<T>(typeName));
        return services;
    }

    /// <summary>
    /// Adds database health checks
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="name">Health check name</param>
    /// <param name="tags">Health check tags</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddDatabaseHealthCheck(this IServiceCollection services, string name = "database", params string[] tags)
    {
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>(name, tags: tags);

        return services;
    }

    private static IServiceCollection AddCorePersistenceCore(IServiceCollection services, PostgreSqlSettings settings)
    {
        // Validate settings
        if (string.IsNullOrEmpty(settings.ConnectionString))
            throw new ArgumentException("PostgreSQL connection string is required");

        // Register connection factories
        services.AddSingleton<IConnectionFactory<IDbConnection>>(provider =>
            new PostgreSqlConnectionFactory(settings.ConnectionString));

        services.AddSingleton<IDbConnectionFactory>(provider =>
            new PostgreSqlConnectionFactory(settings.ConnectionString));

        // Register data store strategy
        services.AddSingleton<IDataStoreStrategy, PostgreSqlDataStoreStrategy>();
        services.AddSingleton<IAdvancedDataStoreStrategy, PostgreSqlDataStoreStrategy>();

        // Register database helpers
        services.AddScoped<IDatabaseHelper, PostgreSqlHelper>();
        services.AddScoped<IPostgreSqlHelper, PostgreSqlHelper>();

        // Register retry helper
        services.AddScoped<PostgreSqlRetryHelper>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<PostgreSqlRetryHelper>>();
            return new PostgreSqlRetryHelper(
                logger,
                settings.MaxRetryCount ?? 3,
                TimeSpan.FromMilliseconds(settings.BaseRetryDelayMilliseconds));
        });

        // Register type mapper and composite type manager
        services.AddSingleton<CompositeTypeManager>();

        // Register bulk operations if enabled
        if (settings.EnableBulkOperations)
        {
            services.AddScoped<PostgreSqlBulkOperations>();
        }

        // Configure bulk operation options
        services.Configure<BulkOperationOptions>(options =>
        {
            options.BatchSize = 1000;
            options.UseTransaction = true;
            options.LogProgress = settings.EnableDetailedLogging;
            options.BulkTimeout = settings.CommandTimeout;
        });

        // Configure database connection options
        services.Configure<DatabaseConnectionOptions>(options =>
        {
            options.ProviderName = "PostgreSQL";
            options.ConnectionString = settings.ConnectionString;
            options.EnableMonitoring = true;
            options.HealthCheckInterval = 30;
        });

        return services;
    }
}

/// <summary>
/// Interface for composite type registration
/// </summary>
public interface ICompositeTypeRegistration
{
    /// <summary>
    /// Registers the composite type
    /// </summary>
    /// <param name="manager">Composite type manager</param>
    void Register(CompositeTypeManager manager);

    /// <summary>
    /// CLR type
    /// </summary>
    Type ClrType { get; }

    /// <summary>
    /// PostgreSQL type name
    /// </summary>
    string TypeName { get; }
}

/// <summary>
/// Implementation of composite type registration
/// </summary>
/// <typeparam name="T">CLR type</typeparam>
public class CompositeTypeRegistration<T> : ICompositeTypeRegistration
{
    /// <summary>
    /// Initializes a new instance of CompositeTypeRegistration
    /// </summary>
    /// <param name="typeName">PostgreSQL type name</param>
    public CompositeTypeRegistration(string typeName)
    {
        TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
        ClrType = typeof(T);
    }

    /// <inheritdoc />
    public Type ClrType { get; }

    /// <inheritdoc />
    public string TypeName { get; }

    /// <inheritdoc />
    public void Register(CompositeTypeManager manager)
    {
        manager.RegisterCompositeType<T>(TypeName);
    }
}

/// <summary>
/// Database health check implementation
/// </summary>
public class DatabaseHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of DatabaseHealthCheck
    /// </summary>
    public DatabaseHealthCheck(IDbConnectionFactory connectionFactory, ILogger<DatabaseHealthCheck> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var isHealthy = await _connectionFactory.TestConnectionAsync(cancellationToken);
            
            if (isHealthy)
            {
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Database connection is healthy");
            }
            else
            {
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Database connection failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Database health check failed", ex);
        }
    }
}
