# Movora.Application

This project contains the application layer for the Movora system, implementing the Clean Architecture pattern with CQRS.

## Structure

### DTOs
- Data Transfer Objects for transferring data between layers

### UseCases
Organized using CQRS pattern:

#### Read Operations
- **Requests**: Query request classes
- **Handlers**: Query handlers implementing business logic
- **Responses**: Query response classes

#### Write Operations
- **Requests**: Command request classes
- **Handlers**: Command handlers implementing business logic
- **Responses**: Command response classes

### Interfaces
- **Repositories**: Repository interfaces for data access
- **DataStores**: Data store strategy interfaces

## Dependencies
- MediatR for CQRS implementation
- FluentValidation for request validation
- Core.CQRS for base classes and extensions
- Core.Persistence for data access patterns

## Key Features
- Clean separation of concerns
- CQRS pattern implementation
- Validation support
- Repository pattern interfaces
- Data store abstraction
