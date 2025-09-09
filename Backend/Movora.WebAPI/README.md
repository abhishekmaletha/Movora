# Movora.WebAPI

This project contains the Web API layer for the Movora system, serving as the entry point for HTTP requests.

## Structure

### Controllers
- API controllers handling HTTP requests
- RESTful endpoints for the application

### Configuration
- **ModulePackage.cs**: Central dependency injection configuration
- Registers all application modules and services

### Configuration Files
- **appsettings.json**: Production configuration
- **appsettings.Development.json**: Development-specific settings

## Key Features
- RESTful API design
- Dependency injection with ModulePackage
- Swagger/OpenAPI documentation
- CORS support
- Authentication and authorization
- MediatR integration for CQRS

## Dependencies
- ASP.NET Core Web API
- MediatR for CQRS
- Swagger for API documentation
- Entity Framework Core for database operations
- Core modules for authentication, logging, persistence, etc.
- Movora.Application for business logic
- Movora.Infrastructure for data access

## API Documentation
When running in development mode, Swagger UI is available at `/swagger` for API exploration and testing.

## Getting Started
1. Configure connection strings in appsettings.json
2. Set up authentication providers (Keycloak)
3. Run database migrations
4. Start the application
