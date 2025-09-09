using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Core.Persistence.Extensions;
using Movora.Infrastructure.SqlModels;
using Movora.Application.Interfaces.Repositories;
using Movora.Application.Interfaces.DataStores;
using Movora.Infrastructure.FlexiSearch;

namespace Movora.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Add DbContext
        services.AddDbContext<MovoraDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection string not found.");
            
            options.UseNpgsql(connectionString);
            
            // Enable sensitive data logging in development
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        });

        // Register Core Persistence services
        services.AddCorePersistence(configuration);

        // Register Data Store Strategy

        // Register Repositories

        // Register FlexiSearch services
        services.AddFlexiSearch(configuration);

        return services;
    }
}
