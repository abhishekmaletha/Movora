using Core.Logging.Abstractions;
using Core.Logging.Configuration;
using Core.Logging.Services;
using Core.Logging.Strategies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Core.Logging.Extensions;

/// <summary>
/// Extension methods for configuring Core.Logging in DI container
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Core.Logging with default configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="configurationSection">The configuration section name (default: "CoreLogging")</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCoreLogging(
        this IServiceCollection services,
        IConfiguration configuration,
        string configurationSection = CoreLoggingOptions.SectionName)
    {
        return services.AddCoreLogging(configuration, configurationSection, null);
    }

    /// <summary>
    /// Adds Core.Logging with custom configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="configurationSection">The configuration section name</param>
    /// <param name="configureOptions">Optional action to configure logging options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCoreLogging(
        this IServiceCollection services,
        IConfiguration configuration,
        string configurationSection,
        Action<CoreLoggingOptions>? configureOptions)
    {
        // Configure logging options
        var loggingSection = configuration.GetSection(configurationSection);
        services.Configure<CoreLoggingOptions>(loggingSection);

        // Apply additional configuration if provided
        if (configureOptions != null)
        {
            services.Configure<CoreLoggingOptions>(configureOptions);
        }

        // Register core services
        services.AddSingleton<ILoggingStrategyFactory, LoggingStrategyFactory>();
        services.AddScoped<IAppLogger, AppLogger>();
        services.AddScoped(typeof(IAppLogger<>), typeof(AppLogger<>));

        // Register logging strategies
        services.AddTransient<ConsoleLoggingStrategy>();
        services.AddTransient<FileLoggingStrategy>();

        // Configure database logging if enabled
        var options = new CoreLoggingOptions();
        loggingSection.Bind(options);
        configureOptions?.Invoke(options);

        if (options.Database.Enabled && !string.IsNullOrEmpty(options.Database.ConnectionString))
        {
            services.AddDbContextFactory<LoggingDbContext>(dbOptions =>
            {
                dbOptions.UseSqlServer(options.Database.ConnectionString, sqlOptions =>
                {
                    sqlOptions.CommandTimeout((int)options.Database.ConnectionTimeout.TotalSeconds);
                });
            });
            
            services.AddTransient<DatabaseLoggingStrategy>();
        }

        // Validate configuration
        options.Validate();

        return services;
    }

    /// <summary>
    /// Adds Core.Logging with console-only configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="minimumLevel">Minimum log level (default: Information)</param>
    /// <param name="applicationName">Application name for logging context</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCoreLoggingConsole(
        this IServiceCollection services,
        Models.LogLevel minimumLevel = Models.LogLevel.Information,
        string applicationName = "Application")
    {
        services.Configure<CoreLoggingOptions>(options =>
        {
            options.MinimumLevel = minimumLevel;
            options.ApplicationName = applicationName;
            options.Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            
            options.Strategies.Enabled = new List<string> { "Console" };
            
            options.Console.Enabled = true;
            options.Console.MinimumLevel = minimumLevel;
            options.Console.UseColors = true;
            
            options.Database.Enabled = false;
            options.File.Enabled = false;
        });

        services.AddSingleton<ILoggingStrategyFactory, LoggingStrategyFactory>();
        services.AddScoped<IAppLogger, AppLogger>();
        services.AddScoped(typeof(IAppLogger<>), typeof(AppLogger<>));
        services.AddTransient<ConsoleLoggingStrategy>();

        return services;
    }

    /// <summary>
    /// Adds Core.Logging with database-only configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="connectionString">Database connection string</param>
    /// <param name="minimumLevel">Minimum log level (default: Warning)</param>
    /// <param name="applicationName">Application name for logging context</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCoreLoggingDatabase(
        this IServiceCollection services,
        string connectionString,
        Models.LogLevel minimumLevel = Models.LogLevel.Warning,
        string applicationName = "Application")
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

        services.Configure<CoreLoggingOptions>(options =>
        {
            options.MinimumLevel = minimumLevel;
            options.ApplicationName = applicationName;
            options.Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            
            options.Strategies.Enabled = new List<string> { "Database" };
            
            options.Console.Enabled = false;
            options.File.Enabled = false;
            
            options.Database.Enabled = true;
            options.Database.MinimumLevel = minimumLevel;
            options.Database.ConnectionString = connectionString;
            options.Database.AutoCreateSqlTable = true;
        });

        services.AddDbContextFactory<LoggingDbContext>(dbOptions =>
        {
            dbOptions.UseSqlServer(connectionString);
        });

        services.AddSingleton<ILoggingStrategyFactory, LoggingStrategyFactory>();
        services.AddScoped<IAppLogger, AppLogger>();
        services.AddScoped(typeof(IAppLogger<>), typeof(AppLogger<>));
        services.AddTransient<DatabaseLoggingStrategy>();

        return services;
    }

    /// <summary>
    /// Adds Core.Logging with multiple strategies
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configure">Action to configure logging options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCoreLogging(
        this IServiceCollection services,
        Action<CoreLoggingOptions> configure)
    {
        services.Configure<CoreLoggingOptions>(configure);

        // Get configuration to determine which strategies to register
        var options = new CoreLoggingOptions();
        configure(options);

        services.AddSingleton<ILoggingStrategyFactory, LoggingStrategyFactory>();
        services.AddScoped<IAppLogger, AppLogger>();
        services.AddScoped(typeof(IAppLogger<>), typeof(AppLogger<>));

        // Register strategies based on configuration
        if (options.Strategies.Enabled.Contains("Console", StringComparer.OrdinalIgnoreCase))
        {
            services.AddTransient<ConsoleLoggingStrategy>();
        }

        if (options.Strategies.Enabled.Contains("File", StringComparer.OrdinalIgnoreCase))
        {
            services.AddTransient<FileLoggingStrategy>();
        }

        if (options.Strategies.Enabled.Contains("Database", StringComparer.OrdinalIgnoreCase) && 
            options.Database.Enabled && 
            !string.IsNullOrEmpty(options.Database.ConnectionString))
        {
            services.AddDbContextFactory<LoggingDbContext>(dbOptions =>
            {
                dbOptions.UseSqlServer(options.Database.ConnectionString);
            });
            
            services.AddTransient<DatabaseLoggingStrategy>();
        }

        // Validate configuration
        options.Validate();

        return services;
    }

    /// <summary>
    /// Adds Core.Logging for development with enhanced console output
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="applicationName">Application name for logging context</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCoreLoggingDevelopment(
        this IServiceCollection services,
        string applicationName = "Application")
    {
        return services.AddCoreLoggingConsole(Models.LogLevel.Debug, applicationName);
    }

    /// <summary>
    /// Adds Core.Logging for production with database and reduced console output
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="connectionString">Database connection string</param>
    /// <param name="applicationName">Application name for logging context</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCoreLoggingProduction(
        this IServiceCollection services,
        string connectionString,
        string applicationName = "Application")
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

        services.Configure<CoreLoggingOptions>(options =>
        {
            options.MinimumLevel = Models.LogLevel.Information;
            options.ApplicationName = applicationName;
            options.Environment = "Production";
            
            options.Strategies.Enabled = new List<string> { "Console", "Database" };
            
            // Console for immediate feedback (errors/warnings only)
            options.Console.Enabled = true;
            options.Console.MinimumLevel = Models.LogLevel.Warning;
            options.Console.UseColors = false;
            
            // Database for all logs
            options.Database.Enabled = true;
            options.Database.MinimumLevel = Models.LogLevel.Information;
            options.Database.ConnectionString = connectionString;
            options.Database.AutoCreateSqlTable = true;
            
            options.File.Enabled = false;
        });

        services.AddDbContextFactory<LoggingDbContext>(dbOptions =>
        {
            dbOptions.UseSqlServer(connectionString);
        });

        services.AddSingleton<ILoggingStrategyFactory, LoggingStrategyFactory>();
        services.AddScoped<IAppLogger, AppLogger>();
        services.AddScoped(typeof(IAppLogger<>), typeof(AppLogger<>));
        services.AddTransient<ConsoleLoggingStrategy>();
        services.AddTransient<DatabaseLoggingStrategy>();

        return services;
    }
}

/// <summary>
/// Extension methods for IHostBuilder
/// </summary>
public static class HostBuilderExtensions
{
    /// <summary>
    /// Adds Core.Logging to the host builder
    /// </summary>
    /// <param name="hostBuilder">The host builder</param>
    /// <param name="configurationSection">The configuration section name</param>
    /// <returns>The host builder for chaining</returns>
    public static IHostBuilder UseCoreLogging(
        this IHostBuilder hostBuilder,
        string configurationSection = CoreLoggingOptions.SectionName)
    {
        return hostBuilder.ConfigureServices((context, services) =>
        {
            services.AddCoreLogging(context.Configuration, configurationSection);
        });
    }

    /// <summary>
    /// Adds Core.Logging to the host builder with custom configuration
    /// </summary>
    /// <param name="hostBuilder">The host builder</param>
    /// <param name="configure">Action to configure logging options</param>
    /// <returns>The host builder for chaining</returns>
    public static IHostBuilder UseCoreLogging(
        this IHostBuilder hostBuilder,
        Action<CoreLoggingOptions> configure)
    {
        return hostBuilder.ConfigureServices((context, services) =>
        {
            services.AddCoreLogging(configure);
        });
    }
}
