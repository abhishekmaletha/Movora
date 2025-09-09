# Movora.Infrastructure

This project contains the infrastructure layer implementation for the Movora system, providing concrete implementations of data access patterns and repository interfaces.

## Architecture Flow

The complete data flow follows this pattern:
```
Controller → Handler → Repository → DataStoreStrategy → SqlHelper → PostgreSQL
```

## Structure

### Repositories
Concrete implementations of repository interfaces defined in the Application layer:

- **MovieRepository**: Handles movie-specific data operations including genre filtering, title search, and recommendation logic
- **SeriesRepository**: Manages series data with episode relationships and genre-based queries  
- **UserRepository**: User management operations including email/external ID lookups and active user filtering

### DataStores
Data store strategy implementations:

- **MovoraDataStore**: Implements `IMovoraDataStore` interface providing:
  - Standard CRUD operations via Entity Framework
  - Vector search capabilities (with PostgreSQL pgvector extension placeholder)
  - Bulk operations for performance
  - Raw SQL execution support
  - Health check functionality

### SqlModels
Entity Framework models representing database tables:
- Movie, Series, Episode, User, Comment, Review, Recommendation, etc.
- MovoraDbContext for database operations

## Key Features

### Repository Pattern
- Clean separation between business logic and data access
- Interface-based design for testability
- Specific repository methods for domain operations

### Data Store Strategy
- Abstraction over different data storage mechanisms
- Support for both Entity Framework and raw SQL
- Vector search capabilities for AI-powered recommendations
- Bulk operations for performance optimization

### Entity Framework Integration
- PostgreSQL provider with Npgsql
- Database context management
- Migration support
- Relationship mapping

## Dependencies

- **Entity Framework Core**: ORM for database operations
- **Npgsql**: PostgreSQL provider for .NET
- **Core.Persistence**: Base classes and interfaces for data access patterns
- **Movora.Application**: Application layer interfaces and contracts

## Configuration

The infrastructure layer is configured through:

1. **Connection Strings**: PostgreSQL database connection in appsettings.json
2. **Service Registration**: Dependency injection setup in ServiceCollectionExtensions
3. **Entity Framework**: DbContext configuration with PostgreSQL provider

## Service Registration

```csharp
services.AddInfrastructure(configuration);
```

This registers:
- MovoraDbContext with PostgreSQL provider
- Core.Persistence services
- Data store strategy implementation
- All repository implementations

## Database Operations

### Standard CRUD
All repositories inherit from GenericRepository providing:
- CreateAsync, UpdateAsync, DeleteAsync
- GetByIdAsync, GetAllAsync
- FindAsync, FirstOrDefaultAsync
- ExistsAsync, CountAsync

### Domain-Specific Operations
Each repository adds specialized methods:
- Movie recommendations based on user watch history
- Genre-based filtering
- Title search capabilities
- Series with episode relationships

### Vector Search
Placeholder implementation for AI-powered search and recommendations:
- Text embedding support
- Similarity search
- Configurable result limits
- Future integration with pgvector extension

## Performance Considerations

- Bulk operations for large data sets
- Efficient query patterns with Entity Framework
- Connection pooling via dependency injection
- Async/await throughout for scalability

## Future Enhancements

1. **Vector Search**: Full pgvector integration for embeddings
2. **Caching**: Redis integration for performance
3. **Read Replicas**: Separate read/write database connections
4. **Monitoring**: Database operation metrics and logging