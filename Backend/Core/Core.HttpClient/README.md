# Core.HttpClient

A comprehensive HttpClient wrapper library that provides convenient extension methods and robust configuration options for .NET applications. Simplify HTTP operations with built-in retry policies, circuit breakers, logging, and error handling.

## Features

- **Simple Extension Methods**: Easy-to-use `GetAsync`, `PostAsync`, `PutAsync`, `PatchAsync`, `DeleteAsync`
- **Typed Responses**: Automatic JSON serialization/deserialization with `ApiResponse<T>`
- **Error Handling**: Comprehensive error handling with detailed response information
- **Retry Policies**: Built-in retry mechanisms with exponential backoff
- **Circuit Breaker**: Fault tolerance with configurable circuit breaker pattern
- **Request Logging**: Automatic request/response logging for debugging
- **Flexible Configuration**: Configure via appsettings.json or code
- **Proxy Support**: Built-in proxy configuration
- **SSL Options**: Custom SSL/TLS certificate handling
- **Easy Integration**: Single extension method call in Program.cs

## Installation

Add this project as a reference to your .NET project:

```xml
<ProjectReference Include="path/to/Core.HttpClient/Core.HttpClient.csproj" />
```

## Quick Start

### 1. Basic Setup

In your `Program.cs`:

```csharp
using Core.HttpClient.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Core.HttpClient with simple configuration
builder.Services.AddCoreHttpClient("https://api.example.com");

var app = builder.Build();
app.Run();
```

### 2. Use in Your Services

```csharp
public class ApiService
{
    private readonly HttpClient _httpClient;

    public ApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ApiResponse<User>> GetUserAsync(int userId)
    {
        return await _httpClient.GetAsync<User>($"/users/{userId}");
    }

    public async Task<ApiResponse<User>> CreateUserAsync(CreateUserRequest request)
    {
        return await _httpClient.PostAsync<CreateUserRequest, User>("/users", request);
    }
}
```

## Extension Methods

### GET Operations

```csharp
// Get as string
var response = await httpClient.GetAsync("/api/data");

// Get and deserialize to type
var response = await httpClient.GetAsync<UserDto>("/api/users/1");

// With custom JSON options
var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
var response = await httpClient.GetAsync<UserDto>("/api/users/1", jsonOptions);
```

### POST Operations

```csharp
// POST without content
var response = await httpClient.PostAsync("/api/trigger");

// POST with JSON content
var request = new CreateUserRequest { Name = "John", Email = "john@example.com" };
var response = await httpClient.PostAsync("/api/users", request);

// POST with JSON content and typed response
var response = await httpClient.PostAsync<CreateUserRequest, UserDto>("/api/users", request);
```

### PUT Operations

```csharp
// PUT with JSON content
var updateRequest = new UpdateUserRequest { Name = "Jane", Email = "jane@example.com" };
var response = await httpClient.PutAsync("/api/users/1", updateRequest);

// PUT with typed response
var response = await httpClient.PutAsync<UpdateUserRequest, UserDto>("/api/users/1", updateRequest);
```

### PATCH Operations

```csharp
// PATCH with JSON content
var patchRequest = new { Name = "New Name" };
var response = await httpClient.PatchAsync("/api/users/1", patchRequest);

// PATCH with typed response
var response = await httpClient.PatchAsync<object, UserDto>("/api/users/1", patchRequest);
```

### DELETE Operations

```csharp
// DELETE request
var response = await httpClient.DeleteAsync("/api/users/1");

// DELETE with typed response
var response = await httpClient.DeleteAsync<DeleteResult>("/api/users/1");
```

## Configuration

### 1. Via appsettings.json

```json
{
  "HttpClient": {
    "BaseAddress": "https://api.example.com",
    "TimeoutSeconds": 30,
    "MaxRetryAttempts": 3,
    "RetryDelayMilliseconds": 1000,
    "UseExponentialBackoff": true,
    "EnableLogging": true,
    "UserAgent": "MyApp/1.0",
    "DefaultHeaders": {
      "X-API-Key": "your-api-key",
      "Accept": "application/json"
    },
    "Ssl": {
      "BypassCertificateValidation": false,
      "ClientCertificatePath": null,
      "AllowedProtocols": ["Tls12", "Tls13"]
    },
    "Proxy": {
      "Address": "proxy.company.com",
      "Port": 8080,
      "Username": "proxyuser",
      "Password": "proxypass",
      "BypassProxyOnLocal": true
    },
    "CircuitBreaker": {
      "Enabled": true,
      "FailureThreshold": 5,
      "DurationOfBreakSeconds": 30
    }
  }
}
```

Then in `Program.cs`:

```csharp
builder.Services.AddCoreHttpClient(builder.Configuration);
```

### 2. Via Code Configuration

```csharp
builder.Services.AddCoreHttpClient(options =>
{
    options.BaseAddress = "https://api.example.com";
    options.TimeoutSeconds = 60;
    options.MaxRetryAttempts = 5;
    options.UseExponentialBackoff = true;
    options.EnableLogging = true;
    
    options.DefaultHeaders.Add("Authorization", "Bearer your-token");
    options.DefaultHeaders.Add("X-Custom-Header", "custom-value");
    
    options.CircuitBreaker.Enabled = true;
    options.CircuitBreaker.FailureThreshold = 3;
    options.CircuitBreaker.DurationOfBreakSeconds = 60;
});
```

### 3. Named HttpClients

```csharp
// Register multiple named clients
builder.Services.AddNamedCoreHttpClient("ApiClient", options =>
{
    options.BaseAddress = "https://api.example.com";
    options.MaxRetryAttempts = 3;
});

builder.Services.AddNamedCoreHttpClient("AuthClient", options =>
{
    options.BaseAddress = "https://auth.example.com";
    options.TimeoutSeconds = 10;
});

// Use in service
public class ApiService
{
    private readonly HttpClient _apiClient;
    private readonly HttpClient _authClient;

    public ApiService(IHttpClientFactory httpClientFactory)
    {
        _apiClient = httpClientFactory.CreateClient("ApiClient");
        _authClient = httpClientFactory.CreateClient("AuthClient");
    }
}
```

## Error Handling

The library provides comprehensive error handling through the `ApiResponse<T>` class:

```csharp
var response = await httpClient.GetAsync<UserDto>("/api/users/1");

if (response.IsSuccess)
{
    var user = response.Data;
    Console.WriteLine($"User: {user.Name}");
}
else
{
    Console.WriteLine($"Error: {response.StatusCode} - {response.ErrorMessage}");
    
    // Handle specific status codes
    switch (response.StatusCode)
    {
        case HttpStatusCode.NotFound:
            Console.WriteLine("User not found");
            break;
        case HttpStatusCode.Unauthorized:
            Console.WriteLine("Authentication required");
            break;
        case HttpStatusCode.InternalServerError:
            Console.WriteLine("Server error occurred");
            break;
    }
}
```

### ApiResponse Properties

```csharp
public class ApiResponse<T>
{
    public T? Data { get; }                    // Response data
    public HttpStatusCode StatusCode { get; }   // HTTP status code
    public bool IsSuccess { get; }              // Whether request succeeded
    public bool IsFailure { get; }              // Whether request failed
    public string? ReasonPhrase { get; }        // HTTP reason phrase
    public string? ErrorMessage { get; }        // Error message if failed
    public DateTime Timestamp { get; }          // Response timestamp
    public int StatusCodeValue { get; }         // Numeric status code
}
```

## Advanced Usage

### Custom JSON Serialization

```csharp
var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = true
};

var response = await httpClient.GetAsync<UserDto>("/api/users/1", jsonOptions);
```

### Retry Policies

```csharp
builder.Services.AddCoreHttpClient(options =>
{
    // Linear retry (same delay each time)
    options.MaxRetryAttempts = 3;
    options.RetryDelayMilliseconds = 1000;
    options.UseExponentialBackoff = false;
    
    // OR Exponential backoff (increasing delay)
    options.MaxRetryAttempts = 5;
    options.RetryDelayMilliseconds = 500;  // Base delay
    options.UseExponentialBackoff = true;  // 500ms, 1s, 2s, 4s, 8s
});
```

### Circuit Breaker Pattern

```csharp
builder.Services.AddCoreHttpClient(options =>
{
    options.CircuitBreaker.Enabled = true;
    options.CircuitBreaker.FailureThreshold = 5;      // Open after 5 failures
    options.CircuitBreaker.DurationOfBreakSeconds = 30; // Stay open for 30 seconds
});
```

### SSL Configuration

```csharp
builder.Services.AddCoreHttpClient(options =>
{
    // For development - bypass certificate validation
    options.Ssl.BypassCertificateValidation = true;
    
    // For production - use client certificate
    options.Ssl.ClientCertificatePath = "/path/to/client.pfx";
    options.Ssl.ClientCertificatePassword = "certificate-password";
    options.Ssl.AllowedProtocols = new[] { "Tls12", "Tls13" };
});
```

### Proxy Configuration

```csharp
builder.Services.AddCoreHttpClient(options =>
{
    options.Proxy = new ProxyOptions
    {
        Address = "proxy.company.com",
        Port = 8080,
        Username = "username",
        Password = "password",
        BypassProxyOnLocal = true,
        BypassList = new[] { "localhost", "127.0.0.1", "*.internal.com" }
    };
});
```

## Integration Examples

### With CQRS Pattern

```csharp
// Query Handler
public class GetUserQueryHandler : QueryHandlerBase<GetUserQuery, UserDto>
{
    private readonly HttpClient _httpClient;

    public GetUserQueryHandler(HttpClient httpClient, ILogger<GetUserQueryHandler> logger) 
        : base(logger)
    {
        _httpClient = httpClient;
    }

    protected override async Task<Result<UserDto>> ExecuteAsync(GetUserQuery query, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync<UserDto>($"/users/{query.UserId}", cancellationToken: cancellationToken);
        
        if (response.IsSuccess)
        {
            return response.Data!;
        }
        
        return response.StatusCode switch
        {
            HttpStatusCode.NotFound => Error.NotFound("User", query.UserId.ToString()),
            HttpStatusCode.Unauthorized => Error.Unauthorized(),
            _ => Error.InternalServerError(response.ErrorMessage)
        };
    }
}
```

### With Background Services

```csharp
public class DataSyncService : BackgroundService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DataSyncService> _logger;

    public DataSyncService(HttpClient httpClient, ILogger<DataSyncService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var response = await _httpClient.GetAsync<SyncData>("/api/sync", cancellationToken: stoppingToken);
                
                if (response.IsSuccess)
                {
                    _logger.LogInformation("Sync completed successfully");
                    // Process data...
                }
                else
                {
                    _logger.LogWarning("Sync failed: {Error}", response.GetErrorMessage());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sync process failed");
            }
            
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
```

### With Minimal APIs

```csharp
app.MapGet("/proxy/users/{id}", async (int id, HttpClient httpClient) =>
{
    var response = await httpClient.GetAsync<UserDto>($"/users/{id}");
    
    return response.IsSuccess 
        ? Results.Ok(response.Data)
        : Results.Problem(response.GetErrorMessage(), statusCode: response.StatusCodeValue);
});

app.MapPost("/proxy/users", async (CreateUserRequest request, HttpClient httpClient) =>
{
    var response = await httpClient.PostAsync<CreateUserRequest, UserDto>("/users", request);
    
    return response.IsSuccess
        ? Results.Created($"/proxy/users/{response.Data.Id}", response.Data)
        : Results.Problem(response.GetErrorMessage(), statusCode: response.StatusCodeValue);
});
```

## Testing

### Unit Testing with Mock

```csharp
[Test]
public async Task GetUserAsync_ShouldReturnUser_WhenUserExists()
{
    // Arrange
    var mockHandler = new Mock<HttpMessageHandler>();
    var expectedUser = new UserDto { Id = 1, Name = "John Doe" };
    var jsonResponse = JsonSerializer.Serialize(expectedUser);
    
    mockHandler.Protected()
        .Setup<Task<HttpResponseMessage>>("SendAsync", 
            ItExpr.IsAny<HttpRequestMessage>(), 
            ItExpr.IsAny<CancellationToken>())
        .ReturnsAsync(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
        });

    var httpClient = new HttpClient(mockHandler.Object)
    {
        BaseAddress = new Uri("https://api.example.com")
    };

    var apiService = new ApiService(httpClient);

    // Act
    var response = await apiService.GetUserAsync(1);

    // Assert
    Assert.IsTrue(response.IsSuccess);
    Assert.AreEqual(expectedUser.Id, response.Data.Id);
    Assert.AreEqual(expectedUser.Name, response.Data.Name);
}
```

### Integration Testing

```csharp
[Test]
public async Task Integration_GetUser_ShouldWork()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddCoreHttpClient("https://jsonplaceholder.typicode.com");
    
    var serviceProvider = services.BuildServiceProvider();
    var httpClient = serviceProvider.GetRequiredService<HttpClient>();

    // Act
    var response = await httpClient.GetAsync<dynamic>("/users/1");

    // Assert
    Assert.IsTrue(response.IsSuccess);
    Assert.IsNotNull(response.Data);
}
```

## Configuration Reference

### HttpClientOptions

| Property | Description | Default |
|----------|-------------|---------|
| BaseAddress | Base URL for all requests | null |
| TimeoutSeconds | Request timeout in seconds | 30 |
| MaxRetryAttempts | Number of retry attempts | 3 |
| RetryDelayMilliseconds | Base retry delay in milliseconds | 1000 |
| UseExponentialBackoff | Use exponential backoff for retries | true |
| EnableLogging | Enable request/response logging | true |
| UserAgent | User agent string | "Core.HttpClient/1.0" |
| DefaultHeaders | Default headers for all requests | {} |
| AutomaticDecompression | Enable automatic response decompression | true |
| UseCookies | Enable cookie handling | true |
| AllowAutoRedirect | Follow redirects automatically | true |
| MaxAutomaticRedirections | Maximum number of redirects | 10 |

### SSL Options

| Property | Description | Default |
|----------|-------------|---------|
| BypassCertificateValidation | Skip certificate validation | false |
| ClientCertificatePath | Path to client certificate | null |
| ClientCertificatePassword | Client certificate password | null |
| AllowedProtocols | Allowed SSL/TLS protocols | ["Tls12", "Tls13"] |

### Circuit Breaker Options

| Property | Description | Default |
|----------|-------------|---------|
| Enabled | Enable circuit breaker | false |
| FailureThreshold | Failures before opening circuit | 5 |
| DurationOfBreakSeconds | Time to keep circuit open | 30 |

## Best Practices

1. **Use Dependency Injection**: Always inject HttpClient rather than creating instances
2. **Configure Timeouts**: Set appropriate timeouts based on your API requirements
3. **Handle Errors Gracefully**: Always check `IsSuccess` before using `Data`
4. **Use Named Clients**: For multiple APIs, use named clients with different configurations
5. **Enable Logging**: Use logging to debug HTTP issues in development
6. **Configure Retries**: Set appropriate retry policies for transient failures
7. **SSL Security**: Never bypass certificate validation in production
8. **Resource Cleanup**: HttpClient instances are properly managed by the DI container

## Troubleshooting

### Common Issues

1. **Timeout Errors**: Increase `TimeoutSeconds` or check network connectivity
2. **SSL Errors**: Verify certificate configuration or temporarily bypass for testing
3. **Proxy Issues**: Check proxy settings and credentials
4. **Serialization Errors**: Verify JSON structure matches your DTOs
5. **Circuit Breaker Activated**: Check failure threshold and wait for circuit to close

### Debug Logging

Enable detailed logging to troubleshoot issues:

```json
{
  "Logging": {
    "LogLevel": {
      "Core.HttpClient": "Debug",
      "System.Net.Http": "Debug"
    }
  }
}
```

## License

This project is licensed under the MIT License.
