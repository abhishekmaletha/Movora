using Movora.Infrastructure;
// using Core.Authentication.Extensions; // Temporarily disabled
using Core.Logging.Extensions;
using Core.HttpClient.Extensions;
using Core.Persistence.Extensions;
using MediatR;
using FluentValidation;
using System.Reflection;

namespace Movora.WebAPI.Configuration;

public static class ModulePackage
{
    public static IServiceCollection AddMovoraModules(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Core modules
        // services.AddKeycloakAuthentication(configuration); // Temporarily disabled
        services.AddCoreLogging(configuration);
        services.AddCoreHttpClient(configuration);
        services.AddCorePersistence(configuration);
        
        // Add Infrastructure layer
        services.AddInfrastructure(configuration);
        
        // Add Application layer
        services.AddApplication();
        
        // Add API-specific services
        services.AddApiServices();
        
        return services;
    }
    
    private static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register MediatR for Application layer
        services.AddMediatR(cfg => 
        {
            cfg.RegisterServicesFromAssembly(Assembly.Load("Movora.Application"));
        });
        
        // Add FluentValidation
        services.AddValidatorsFromAssembly(Assembly.Load("Movora.Application"));
        
        return services;
    }
    
    private static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        
        // Add CORS
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", builder =>
            {
                builder
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });
        
        return services;
    }
}
