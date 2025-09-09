# Core

Core repository patterns and data store abstractions for the Movora application.

## Overview

This project provides the fundamental repository patterns and interfaces used throughout the Movora application. It implements the Repository pattern with a strategic approach to data store access.

## Components

### Entity Interface
- **`IEntity<TId>`**: Base interface for all entities with an identifier

### Repository Interfaces
- **`IQueryRepository<TId, TEntity>`**: Interface for read operations
  - `GetItemAsync(TId id, object? parameters = null)`
  - `GetAllAsync(object? parameters = null)`

- **`ICommandRepository<TId, TEntity>`**: Interface for write operations
  - `ExecuteAsync(object? parameters = null)`
  - `InsertItemAsync(TEntity item, object? parameters = null)`
  - `DeleteItemAsync(TId id, object? parameters = null)`
  - `UpdateItemAsync(TId id, TEntity item, object? parameters = null)`
  - `UpsertItemAsync(TId id, TEntity item, object? parameters = null)`

- **`ICommandQueryRepository<TId, TEntity>`**: Interface for operations that return entities
  - `InsertItemWithResultAsync(TEntity item, object parameters)`

- **`IGenericRepository<TId, TEntity>`**: Combined interface that includes all operations

### Implementation
- **`GenericRepository<TId, TEntity, TPersistanceEntity>`**: Generic repository implementation that delegates operations to a data store strategy

### Strategy Pattern
- **`IDataStoreStrategy<TId, TEntity, TPersistenceEntity>`**: Interface for data store strategy implementations
  - Extends `IGenericRepository<TId, TEntity>`
  - Provides methods to override collection and database names

## Usage

```csharp
// Define your entity
public class User : IEntity<int>
{
    public int Id { get; set; }
    public string Name { get; set; }
}

// Implement your data store strategy
public class UserDataStoreStrategy : IDataStoreStrategy<int, User, UserDbModel>
{
    // Implementation details...
}

// Use with dependency injection
public class UserService
{
    private readonly IGenericRepository<int, User> _repository;
    
    public UserService(IGenericRepository<int, User> repository)
    {
        _repository = repository;
    }
    
    public async Task<User> GetUserAsync(int id)
    {
        return await _repository.GetItemAsync(id);
    }
}
```

## Architecture

This project follows the Repository pattern combined with the Strategy pattern to provide:
- **Separation of Concerns**: Clear separation between business logic and data access
- **Testability**: Easy to mock and test repository operations
- **Flexibility**: Support for different data store implementations
- **Consistency**: Uniform interface for all data operations

## Dependencies

- .NET 8.0
- No external package dependencies (pure abstraction layer)
