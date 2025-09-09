# Core.Persistence

A comprehensive database persistence layer library for .NET Core applications with first-class PostgreSQL support and extensible architecture for multiple database providers.

## 🚀 Features

### 🔧 **Core Components**
- **Connection Management** - Robust connection factory with pooling and health checks
- **Database Helpers** - High-level abstractions for common database operations
- **Data Store Strategy** - Strategy pattern for different database providers
- **Type Mapping** - Automatic mapping between C# types and PostgreSQL types
- **Bulk Operations** - High-performance bulk insert, update, and upsert operations
- **Retry Logic** - Built-in retry mechanisms for transient database errors

### 🐘 **PostgreSQL Features**
- **Native PostgreSQL Support** - Full support for PostgreSQL-specific features
- **Array Types** - Native PostgreSQL array support
- **Composite Types** - User-defined composite type mapping
- **JSON/JSONB** - Native JSON and JSONB column support
- **COPY Operations** - High-performance bulk data loading using PostgreSQL COPY
- **Functions** - Execute PostgreSQL functions with ease

### 📊 **Advanced Operations**
- **Transaction Management** - Comprehensive transaction support with isolation levels
- **Health Checks** - Built-in database health monitoring
- **Performance Monitoring** - Query performance tracking and logging
- **Connection Pooling** - Efficient database connection management

## 🏗️ Installation

Add the package reference to your project:

```xml
<PackageReference Include="Core.Persistence" Version="1.0.0" />
```

## ⚡ Quick Start

### 1. **Simple Setup**

```csharp
using Core.Persistence.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Core.Persistence with PostgreSQL
builder.Services.AddCorePersistencePostgreSQL(
    "Host=localhost;Database=mydb;Username=user;Password=password");

var app = builder.Build();
app.Run();
```

### 2. **Configuration-Based Setup**

```csharp
using Core.Persistence.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Core.Persistence using configuration
builder.Services.AddCorePersistence(builder.Configuration);

var app = builder.Build();
app.Run();
```

**appsettings.json:**
```json
{
  "PostgreSQL": {
    "ConnectionString": "Host=localhost;Database=mydb;Username=user;Password=password",
    "MaxRetryCount": 3,
    "EnableBulkOperations": true,
    "EnableDetailedLogging": true,
    "CommandTimeout": 30
  }
}
```

### 3. **Advanced Setup with Health Checks**

```csharp
using Core.Persistence.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddCorePersistence(builder.Configuration)
    .AddBulkOperations()
    .AddCompositeTypes()
    .AddDatabaseHealthCheck();

// Add health check endpoints
builder.Services.AddHealthChecks();

var app = builder.Build();

// Map health check endpoints
app.MapHealthChecks("/health");

app.Run();
```

## 📖 Usage Examples

### **Basic Database Operations**

```csharp
public class UserService
{
    private readonly IPostgreSqlHelper _dbHelper;

    public UserService(IPostgreSqlHelper dbHelper)
    {
        _dbHelper = dbHelper;
    }

    public async Task<User?> GetUserAsync(int userId)
    {
        const string sql = "SELECT * FROM users WHERE id = @UserId";
        return await _dbHelper.QuerySingleOrDefaultAsync<User>(sql, new { UserId = userId });
    }

    public async Task<IEnumerable<User>> GetActiveUsersAsync()
    {
        const string sql = "SELECT * FROM users WHERE is_active = true";
        return await _dbHelper.QueryAsync<User>(sql);
    }

    public async Task<int> CreateUserAsync(User user)
    {
        const string sql = @"
            INSERT INTO users (name, email, created_at) 
            VALUES (@Name, @Email, @CreatedAt) 
            RETURNING id";
        
        return await _dbHelper.ExecuteScalarAsync<int>(sql, user);
    }
}
```

### **Transaction Management**

```csharp
public async Task<bool> TransferFundsAsync(int fromAccountId, int toAccountId, decimal amount)
{
    return await _dbHelper.ExecuteInTransactionAsync(async (transaction, cancellationToken) =>
    {
        // Debit from account
        const string debitSql = "UPDATE accounts SET balance = balance - @Amount WHERE id = @AccountId";
        await _dbHelper.ExecuteAsync(debitSql, new { Amount = amount, AccountId = fromAccountId });

        // Credit to account
        const string creditSql = "UPDATE accounts SET balance = balance + @Amount WHERE id = @AccountId";
        await _dbHelper.ExecuteAsync(creditSql, new { Amount = amount, AccountId = toAccountId });

        // Log transaction
        const string logSql = "INSERT INTO transactions (from_account, to_account, amount, created_at) VALUES (@From, @To, @Amount, @CreatedAt)";
        await _dbHelper.ExecuteAsync(logSql, new { From = fromAccountId, To = toAccountId, Amount = amount, CreatedAt = DateTime.UtcNow });

        return true;
    });
}
```

### **Bulk Operations**

```csharp
public class ProductService
{
    private readonly PostgreSqlBulkOperations _bulkOps;

    public ProductService(PostgreSqlBulkOperations bulkOps)
    {
        _bulkOps = bulkOps;
    }

    public async Task<long> ImportProductsAsync(IEnumerable<Product> products)
    {
        using var connection = _connectionFactory.Connection;
        await connection.OpenAsync();

        return await _bulkOps.BulkInsertAsync(connection, "products", products);
    }

    public async Task<long> UpsertProductsAsync(IEnumerable<Product> products)
    {
        using var connection = _connectionFactory.Connection;
        await connection.OpenAsync();

        // Upsert based on product code
        return await _bulkOps.BulkUpsertAsync(
            connection, 
            "products", 
            products, 
            new[] { "product_code" });
    }
}
```

### **PostgreSQL Functions**

```csharp
public async Task<decimal> CalculateOrderTotalAsync(int orderId)
{
    return await _dbHelper.ExecuteFunctionAsync<decimal>(
        "calculate_order_total", 
        new { order_id = orderId });
}

public async Task<IEnumerable<OrderSummary>> GetOrderSummariesAsync(DateTime fromDate, DateTime toDate)
{
    return await _dbHelper.ExecuteFunctionMultipleAsync<OrderSummary>(
        "get_order_summaries", 
        new { from_date = fromDate, to_date = toDate });
}
```

### **Array and JSON Support**

```csharp
public class TagService
{
    public async Task<Product?> GetProductWithTagsAsync(int productId)
    {
        const string sql = @"
            SELECT id, name, tags, metadata 
            FROM products 
            WHERE id = @ProductId";
        
        return await _dbHelper.QuerySingleOrDefaultAsync<Product>(sql, new { ProductId = productId });
    }

    public async Task<IEnumerable<Product>> FindProductsByTagsAsync(string[] tags)
    {
        const string sql = @"
            SELECT * FROM products 
            WHERE tags && @Tags";  -- PostgreSQL array overlap operator
        
        return await _dbHelper.QueryAsync<Product>(sql, new { Tags = tags });
    }
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    [PgArray]
    public string[] Tags { get; set; } = Array.Empty<string>();
    
    [PgJson(useJsonb: true)]
    public Dictionary<string, object> Metadata { get; set; } = new();
}
```

### **Composite Types**

```csharp
// Define composite type
[PgName("address_type")]
public class Address
{
    [PostgreSqlComposite(1)]
    public string Street { get; set; } = string.Empty;
    
    [PostgreSqlComposite(2)]
    public string City { get; set; } = string.Empty;
    
    [PostgreSqlComposite(3)]
    public string State { get; set; } = string.Empty;
    
    [PostgreSqlComposite(4)]
    public string ZipCode { get; set; } = string.Empty;
}

// Register composite type
builder.Services.RegisterCompositeType<Address>("address_type");

// Use composite type
public async Task CreateUserWithAddressAsync(User user, Address address)
{
    const string sql = @"
        INSERT INTO users (name, email, address) 
        VALUES (@Name, @Email, @Address::address_type)";
    
    var compositeAddress = _dbHelper.CreateCompositeTypeParameter(
        new[] { address }, "address_type");
    
    await _dbHelper.ExecuteAsync(sql, new { 
        user.Name, 
        user.Email, 
        Address = compositeAddress 
    });
}
```

## 🔧 Configuration

### **Complete Configuration Example**

```json
{
  "PostgreSQL": {
    "ConnectionString": "Host=localhost;Database=myapp;Username=appuser;Password=secret",
    "MaxRetryCount": 3,
    "MaxRetryDelayDuration": 30,
    "BaseRetryDelayMilliseconds": 1000,
    "UseExponentialBackoff": true,
    "EnableArraySupport": true,
    "EnableCompositeTypes": true,
    "EnableBulkOperations": true,
    "EnableConnectionPooling": true,
    "MinPoolSize": 1,
    "MaxPoolSize": 100,
    "ConnectionLifetime": 3600,
    "CommandTimeout": 30,
    "ConnectionTimeout": 15,
    "EnableDetailedLogging": false,
    "LogParameters": false,
    "LogSlowQueries": true,
    "SlowQueryThreshold": 1000,
    "SslMode": "Prefer",
    "TrustServerCertificate": false,
    "CompositeTypeMappings": {
      "AddressType": "address_type",
      "ContactType": "contact_type"
    }
  }
}
```

### **Environment-Specific Configuration**

```csharp
// Development
builder.Services.AddCorePersistencePostgreSQL(options =>
{
    options.ConnectionString = "Host=localhost;Database=myapp_dev;Username=dev;Password=dev";
    options.EnableDetailedLogging = true;
    options.LogParameters = true;
    options.MaxRetryCount = 1;
});

// Production
builder.Services.AddCorePersistencePostgreSQL(options =>
{
    options.ConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
    options.EnableDetailedLogging = false;
    options.LogParameters = false;
    options.MaxRetryCount = 5;
    options.UseExponentialBackoff = true;
});
```

## 🛡️ Error Handling and Retry Logic

The library includes built-in retry logic for transient database errors:

```csharp
// Automatic retry for transient errors
try 
{
    var result = await _dbHelper.QueryAsync<User>("SELECT * FROM users");
}
catch (PostgresException ex) when (ex.IsRetriableError())
{
    // Will be automatically retried up to MaxRetryCount times
}
```

**Retriable Error Conditions:**
- Connection failures
- Timeout errors  
- Deadlock detection
- Serialization failures
- Database shutdowns

## 📊 Performance Features

### **Connection Pooling**
```csharp
builder.Services.AddCorePersistencePostgreSQL(options =>
{
    options.EnableConnectionPooling = true;
    options.MinPoolSize = 5;
    options.MaxPoolSize = 50;
    options.ConnectionLifetime = 1800; // 30 minutes
});
```

### **Bulk Operations Performance**
```csharp
builder.Services.AddBulkOperations(options =>
{
    options.BatchSize = 5000;
    options.UseTransaction = true;
    options.UseParallelProcessing = true;
    options.MaxDegreeOfParallelism = Environment.ProcessorCount;
});
```

### **Query Performance Monitoring**
```csharp
builder.Services.AddCorePersistencePostgreSQL(options =>
{
    options.LogSlowQueries = true;
    options.SlowQueryThreshold = 500; // Log queries taking > 500ms
});
```

## 🏥 Health Checks

Monitor database health in your applications:

```csharp
// Add health checks
builder.Services.AddDatabaseHealthCheck("database", "ready", "live");

// Configure health check endpoints
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});
```

## 🔍 Logging

The library provides comprehensive logging:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Core.Persistence": "Debug",
      "Core.Persistence.Helpers.PostgreSqlHelper": "Information"
    }
  }
}
```

**Log Categories:**
- `Core.Persistence.ConnectionFactory` - Connection management
- `Core.Persistence.Helpers.PostgreSqlHelper` - Database operations
- `Core.Persistence.BulkOperations` - Bulk operation progress
- `Core.Persistence.Retry` - Retry logic execution

## 🧪 Testing

For unit testing, use the minimal setup:

```csharp
// Test setup
services.AddCorePersistenceMinimal("Host=localhost;Database=testdb;Username=test;Password=test");

// Or mock the interfaces
services.AddScoped<IDatabaseHelper, MockDatabaseHelper>();
```

## 🔒 Security Considerations

- **SQL Injection Protection** - All operations use parameterized queries
- **Connection Security** - SSL/TLS support with certificate validation
- **Credential Management** - Support for connection string security
- **Least Privilege** - Configurable database permissions

## 📋 Requirements

- **.NET 8.0** or later
- **PostgreSQL 12** or later
- **Npgsql 8.0** or later

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License.

## 🆘 Support

For issues and questions:
- Create an issue in the repository
- Check the documentation
- Review the examples

---

**Core.Persistence** - Professional database persistence for .NET applications 🚀
