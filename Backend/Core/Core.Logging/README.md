# Core.Logging

A comprehensive logging library for .NET applications that supports multiple logging strategies (console, database, file) with easy configuration and integration.

## Features

- **Multiple Logging Strategies**: Console, Database, File logging
- **Easy Integration**: Single extension method call in Program.cs
- **Flexible Configuration**: Configure via appsettings.json or code
- **Structured Logging**: Support for structured logging with properties and context
- **Async Logging**: Asynchronous logging for better performance
- **Scoped Logging**: Support for logging scopes and correlation IDs
- **Type-safe Logging**: Generic logger interface for type-specific logging
- **Batching Support**: Efficient batch processing for database logging
- **Auto-rotation**: File logging with automatic rotation and cleanup

## Installation

Add this project as a reference to your .NET project:

```xml
<ProjectReference Include="path/to/Core.Logging/Core.Logging.csproj" />
```

## Quick Start

### 1. Simple Console Logging

```csharp
using Core.Logging.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add simple console logging
builder.Services.AddCoreLoggingConsole();

var app = builder.Build();

// Use in your controllers/services
app.MapGet("/", (IAppLogger logger) =>
{
    logger.LogInformation("Hello from Core.Logging!");
    return "Hello World!";
});

app.Run();
```

### 2. Configuration-based Setup

Add to your `appsettings.json`:

```json
{
  "CoreLogging": {
    "MinimumLevel": "Information",
    "ApplicationName": "MyWebApi",
    "Environment": "Development",
    "Strategies": {
      "Enabled": ["Console", "Database"]
    },
    "Console": {
      "Enabled": true,
      "MinimumLevel": "Information",
      "UseColors": true
    },
    "Database": {
      "Enabled": true,
      "MinimumLevel": "Warning",
      "ConnectionString": "Server=.;Database=Logs;Trusted_Connection=true;",
      "TableName": "ApplicationLogs",
      "AutoCreateSqlTable": true
    }
  }
}
```

Then in your `Program.cs`:

```csharp
using Core.Logging.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Core.Logging with configuration
builder.Services.AddCoreLogging(builder.Configuration);

var app = builder.Build();
app.Run();
```

### 3. Environment-specific Configuration

```csharp
using Core.Logging.Extensions;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    // Enhanced console logging for development
    builder.Services.AddCoreLoggingDevelopment("MyApp");
}
else
{
    // Database + limited console for production
    builder.Services.AddCoreLoggingProduction(
        connectionString: builder.Configuration.GetConnectionString("LoggingDb")!,
        applicationName: "MyApp");
}

var app = builder.Build();
app.Run();
```

## Usage Examples

### Basic Logging

```csharp
[ApiController]
[Route("api/[controller]")]
public class WeatherController : ControllerBase
{
    private readonly IAppLogger<WeatherController> _logger;

    public WeatherController(IAppLogger<WeatherController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult GetWeather()
    {
        _logger.LogInformation("Getting weather data");
        
        try
        {
            var weather = GetWeatherData();
            _logger.LogInformation("Successfully retrieved weather data for {Count} locations", weather.Count);
            return Ok(weather);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve weather data");
            return StatusCode(500, "Internal server error");
        }
    }
}
```

### Structured Logging with Context

```csharp
public class UserService
{
    private readonly IAppLogger<UserService> _logger;

    public UserService(IAppLogger<UserService> logger)
    {
        _logger = logger;
    }

    public async Task<User> CreateUserAsync(CreateUserRequest request)
    {
        using var scope = _logger.BeginScope("CreateUser");
        
        _logger.LogInformation("Creating user", new 
        { 
            Email = request.Email, 
            RequestId = request.Id,
            UserAgent = HttpContext.Current?.Request.UserAgent 
        });

        try
        {
            var user = await _userRepository.CreateAsync(request);
            
            _logger.LogInformation("User created successfully", new 
            { 
                UserId = user.Id, 
                Email = user.Email 
            });
            
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user", new 
            { 
                Email = request.Email, 
                RequestId = request.Id 
            });
            throw;
        }
    }
}
```

### Custom Configuration

```csharp
using Core.Logging.Extensions;
using Core.Logging.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCoreLogging(options =>
{
    options.MinimumLevel = LogLevel.Information;
    options.ApplicationName = "MyCustomApp";
    options.Environment = builder.Environment.EnvironmentName;
    options.IncludeMachineName = true;
    options.IncludeProcessInfo = true;
    
    // Enable multiple strategies
    options.Strategies.Enabled = new List<string> { "Console", "File", "Database" };
    
    // Console configuration
    options.Console.Enabled = true;
    options.Console.UseColors = true;
    options.Console.OutputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Category}: {Message}{NewLine}{Exception}";
    
    // File configuration
    options.File.Enabled = true;
    options.File.Path = "logs/app-.log";
    options.File.RollingInterval = RollingInterval.Day;
    options.File.FileSizeLimitBytes = 100 * 1024 * 1024; // 100MB
    options.File.RetainedFileCountLimit = 30;
    
    // Database configuration
    options.Database.Enabled = true;
    options.Database.ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
    options.Database.MinimumLevel = LogLevel.Warning;
    options.Database.BatchSize = 100;
    
    // Default properties added to all logs
    options.DefaultProperties.Add("Version", "1.0.0");
    options.DefaultProperties.Add("ServiceName", "WeatherService");
});

var app = builder.Build();
app.Run();
```

## Configuration Options

### CoreLoggingOptions

| Property | Description | Default |
|----------|-------------|---------|
| MinimumLevel | Minimum log level | Information |
| ApplicationName | Application name for context | "Application" |
| Environment | Environment name | "Development" |
| IncludeMachineName | Include machine name in logs | true |
| IncludeProcessInfo | Include process/thread IDs | false |
| IncludeScopes | Include scope information | true |
| DefaultProperties | Default properties for all logs | {} |

### Strategy Configuration

#### Console Logging
```json
{
  "Console": {
    "Enabled": true,
    "MinimumLevel": "Information",
    "UseColors": true,
    "OutputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
    "IncludeThreadInfo": false
  }
}
```

#### Database Logging
```json
{
  "Database": {
    "Enabled": true,
    "MinimumLevel": "Warning",
    "ConnectionString": "Server=.;Database=Logs;Trusted_Connection=true;",
    "TableName": "Logs",
    "SchemaName": "dbo",
    "AutoCreateSqlTable": true,
    "BatchSize": 50,
    "BatchTimeout": "00:00:10",
    "ConnectionTimeout": "00:00:30"
  }
}
```

#### File Logging
```json
{
  "File": {
    "Enabled": true,
    "MinimumLevel": "Information",
    "Path": "logs/log-.txt",
    "RollingInterval": "Day",
    "FileSizeLimitBytes": 104857600,
    "RetainedFileCountLimit": 31,
    "OutputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
  }
}
```

## Database Schema

The database logging strategy creates a table with the following schema:

```sql
CREATE TABLE [dbo].[Logs] (
    [Id] NVARCHAR(50) NOT NULL PRIMARY KEY,
    [Timestamp] DATETIME2 NOT NULL,
    [Level] NVARCHAR(20) NOT NULL,
    [Message] NVARCHAR(MAX) NOT NULL,
    [MessageTemplate] NVARCHAR(MAX) NULL,
    [Exception] NVARCHAR(MAX) NULL,
    [Category] NVARCHAR(200) NULL,
    [Application] NVARCHAR(100) NULL,
    [Environment] NVARCHAR(50) NULL,
    [MachineName] NVARCHAR(100) NULL,
    [UserId] NVARCHAR(100) NULL,
    [CorrelationId] NVARCHAR(100) NULL,
    [Properties] NVARCHAR(MAX) NULL,
    [Scope] NVARCHAR(500) NULL,
    [ThreadId] INT NULL,
    [ProcessId] INT NULL
);

-- Indexes for performance
CREATE INDEX IX_Logs_Timestamp ON Logs(Timestamp);
CREATE INDEX IX_Logs_Level ON Logs(Level);
CREATE INDEX IX_Logs_Category ON Logs(Category);
CREATE INDEX IX_Logs_CorrelationId ON Logs(CorrelationId);
CREATE INDEX IX_Logs_Application_Environment ON Logs(Application, Environment);
```

## Advanced Features

### Scoped Logging

```csharp
public async Task ProcessOrderAsync(int orderId)
{
    using var scope = _logger.BeginScope($"ProcessOrder-{orderId}");
    
    _logger.LogInformation("Starting order processing");
    
    // All logs within this scope will include the scope information
    await ValidateOrder(orderId);
    await ProcessPayment(orderId);
    await ShipOrder(orderId);
    
    _logger.LogInformation("Order processing completed");
}
```

### Correlation IDs

The logger automatically supports correlation IDs for request tracking. You can set them manually or they'll be generated automatically.

### Custom Properties

```csharp
_logger.LogInformation("User login", new 
{
    UserId = user.Id,
    UserAgent = Request.Headers.UserAgent,
    IpAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString(),
    LoginTimestamp = DateTime.UtcNow
});
```

### Error Handling

The logging system is designed to be resilient:
- Failed database writes don't affect application flow
- File write errors are handled gracefully
- Console write errors are silently ignored
- Strategies can be disabled without code changes

## Performance Considerations

- **Async Logging**: All strategies support asynchronous logging
- **Batching**: Database logging uses batching for efficiency
- **Buffering**: File logging uses buffering with periodic flushes
- **Lazy Evaluation**: Log messages are only formatted when needed
- **Thread Safety**: All components are thread-safe

## Integration with ASP.NET Core

### Middleware for Correlation IDs

```csharp
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IAppLogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, IAppLogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() 
                          ?? Guid.NewGuid().ToString();
        
        context.Items["CorrelationId"] = correlationId;
        
        using var scope = _logger.BeginScope($"Request-{correlationId}");
        
        _logger.LogInformation("Request started", new 
        { 
            Method = context.Request.Method,
            Path = context.Request.Path,
            CorrelationId = correlationId
        });

        await _next(context);
        
        _logger.LogInformation("Request completed", new 
        { 
            StatusCode = context.Response.StatusCode,
            CorrelationId = correlationId
        });
    }
}
```

## Best Practices

1. **Use typed loggers**: `IAppLogger<T>` for better categorization
2. **Include context**: Add relevant properties to log entries
3. **Use appropriate levels**: Debug for development, Warning/Error for production issues
4. **Scope related operations**: Use scopes for request/operation tracking
5. **Avoid logging sensitive data**: Be careful with PII and security-sensitive information
6. **Configure different levels per strategy**: Console for immediate feedback, database for persistence
7. **Monitor performance**: Be aware of logging overhead in high-throughput scenarios

## Troubleshooting

### Common Issues

1. **Database connection issues**: Check connection string and permissions
2. **File permission errors**: Ensure write permissions to log directory
3. **High memory usage**: Reduce batch sizes or buffer sizes
4. **Missing logs**: Check minimum log levels and strategy configuration

### Debug Configuration

```json
{
  "CoreLogging": {
    "MinimumLevel": "Debug",
    "Console": {
      "Enabled": true,
      "MinimumLevel": "Debug",
      "IncludeThreadInfo": true
    }
  }
}
```

## License

This project is licensed under the MIT License.
