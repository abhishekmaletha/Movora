using Core.Authentication.Configuration;
using Core.Authentication.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Security.Claims;

namespace Core.Authentication.Extensions;

/// <summary>
/// Extension methods for configuring Keycloak authentication in DI container
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Keycloak JWT Bearer authentication to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="configurationSection">The configuration section name (default: "Keycloak")</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        string configurationSection = KeycloakOptions.SectionName)
    {
        return services.AddKeycloakAuthentication(configuration, configurationSection, null);
    }

    /// <summary>
    /// Adds Keycloak JWT Bearer authentication to the service collection with custom configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="configurationSection">The configuration section name</param>
    /// <param name="configureOptions">Optional action to configure Keycloak options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        string configurationSection,
        Action<KeycloakOptions>? configureOptions)
    {
        // Configure Keycloak options
        var keycloakSection = configuration.GetSection(configurationSection);
        services.Configure<KeycloakOptions>(keycloakSection);

        // Apply additional configuration if provided
        if (configureOptions != null)
        {
            services.Configure<KeycloakOptions>(configureOptions);
        }

        // Configure JWT validation options (with defaults)
        services.Configure<JwtValidationOptions>(options => { });

        // Register services
        services.AddSingleton<IKeycloakTokenValidator, KeycloakTokenValidator>();
        services.AddScoped<IKeycloakUserService, KeycloakUserService>();

        // Configure authentication
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            var serviceProvider = services.BuildServiceProvider();
            var keycloakOptions = serviceProvider.GetRequiredService<IOptions<KeycloakOptions>>().Value;
            var jwtValidationOptions = serviceProvider.GetRequiredService<IOptions<JwtValidationOptions>>().Value;

            // Validate configuration
            keycloakOptions.Validate();

            // Configure JWT Bearer options
            options.Authority = keycloakOptions.GetRealmAuthority();
            options.Audience = keycloakOptions.Audience ?? keycloakOptions.ClientId;
            options.RequireHttpsMetadata = keycloakOptions.RequireHttpsMetadata;
            options.SaveToken = keycloakOptions.SaveTokens;
            options.MetadataAddress = keycloakOptions.GetMetadataAddress();

            // Configure token validation
            options.TokenValidationParameters = jwtValidationOptions.ToTokenValidationParameters(keycloakOptions);

            // Configure events for custom handling
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var userService = context.HttpContext.RequestServices.GetRequiredService<IKeycloakUserService>();
                    await userService.EnrichUserClaimsAsync(context.Principal!, context.HttpContext.RequestAborted);
                },
                OnAuthenticationFailed = context =>
                {
                    // Log authentication failures here if needed
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    // Customize challenge response if needed
                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }

    /// <summary>
    /// Adds Keycloak authentication with custom JWT validation options
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="configureKeycloak">Action to configure Keycloak options</param>
    /// <param name="configureJwtValidation">Action to configure JWT validation options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<KeycloakOptions> configureKeycloak,
        Action<JwtValidationOptions> configureJwtValidation)
    {
        // Configure options
        services.Configure<KeycloakOptions>(configuration.GetSection(KeycloakOptions.SectionName));
        services.Configure(configureKeycloak);
        services.Configure(configureJwtValidation);

        // Register services
        services.AddSingleton<IKeycloakTokenValidator, KeycloakTokenValidator>();
        services.AddScoped<IKeycloakUserService, KeycloakUserService>();

        // Configure authentication
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var serviceProvider = services.BuildServiceProvider();
                var keycloakOptions = serviceProvider.GetRequiredService<IOptions<KeycloakOptions>>().Value;
                var jwtValidationOptions = serviceProvider.GetRequiredService<IOptions<JwtValidationOptions>>().Value;

                keycloakOptions.Validate();

                options.Authority = keycloakOptions.GetRealmAuthority();
                options.Audience = keycloakOptions.Audience ?? keycloakOptions.ClientId;
                options.RequireHttpsMetadata = keycloakOptions.RequireHttpsMetadata;
                options.SaveToken = keycloakOptions.SaveTokens;
                options.MetadataAddress = keycloakOptions.GetMetadataAddress();
                options.TokenValidationParameters = jwtValidationOptions.ToTokenValidationParameters(keycloakOptions);
            });

        return services;
    }

    /// <summary>
    /// Adds Keycloak authentication with manual configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="keycloakOptions">Keycloak configuration options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services,
        KeycloakOptions keycloakOptions)
    {
        keycloakOptions.Validate();

        services.Configure<KeycloakOptions>(options =>
        {
            options.Authority = keycloakOptions.Authority;
            options.Realm = keycloakOptions.Realm;
            options.ClientId = keycloakOptions.ClientId;
            options.ClientSecret = keycloakOptions.ClientSecret;
            options.RequireHttpsMetadata = keycloakOptions.RequireHttpsMetadata;
            options.SaveTokens = keycloakOptions.SaveTokens;
            options.Audience = keycloakOptions.Audience;
            options.ValidIssuers = keycloakOptions.ValidIssuers;
            options.ClockSkew = keycloakOptions.ClockSkew;
            options.MetadataAddress = keycloakOptions.MetadataAddress;
        });

        services.Configure<JwtValidationOptions>(options => { });

        services.AddSingleton<IKeycloakTokenValidator, KeycloakTokenValidator>();
        services.AddScoped<IKeycloakUserService, KeycloakUserService>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwtValidationOptions = new JwtValidationOptions();
                
                options.Authority = keycloakOptions.GetRealmAuthority();
                options.Audience = keycloakOptions.Audience ?? keycloakOptions.ClientId;
                options.RequireHttpsMetadata = keycloakOptions.RequireHttpsMetadata;
                options.SaveToken = keycloakOptions.SaveTokens;
                options.MetadataAddress = keycloakOptions.GetMetadataAddress();
                options.TokenValidationParameters = jwtValidationOptions.ToTokenValidationParameters(keycloakOptions);
            });

        return services;
    }
}
