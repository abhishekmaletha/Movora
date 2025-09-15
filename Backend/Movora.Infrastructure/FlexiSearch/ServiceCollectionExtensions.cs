using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Movora.Domain.FlexiSearch;

namespace Movora.Infrastructure.FlexiSearch;

/// <summary>
/// Service registration extensions for FlexiSearch functionality
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all FlexiSearch services including LLM providers, TMDb client, and ranking
    /// </summary>
    /// <param name="services">Service collection to register services with</param>
    /// <param name="configuration">Configuration to read settings from</param>
    /// <returns>Service collection for method chaining</returns>
    public static IServiceCollection AddFlexiSearch(this IServiceCollection services, IConfiguration configuration)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        // Register core FlexiSearch services
        services.AddScoped<IRanker, EnhancedRanker>();
        services.AddScoped<ILlmSearch, LlmSearchSelector>();
        
        // Register handlers
        //services.AddScoped<FlexiSearchCommandHandler>();
        //services.AddScoped<EnhancedFlexiSearchCommandHandler>();
        //services.AddScoped<MinimalFlexiSearchCommandHandler>();
        
        // Register individual LLM provider implementations
        services.AddScoped<OpenAiLlmSearch>();
        services.AddScoped<GroqLlmSearch>();

        // Register TMDb client
        services.AddScoped<ITmdbClient, TmdbClient>();

        // Configure HttpClients for external API calls
        RegisterHttpClients(services, configuration);

        return services;
    }

    private static void RegisterHttpClients(IServiceCollection services, IConfiguration configuration)
    {
        // HttpClient for OpenAI
        services.AddHttpClient<OpenAiLlmSearch>(client =>
        {
            client.BaseAddress = new Uri("https://api.openai.com/v1/");
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "Movora-FlexiSearch/1.0");
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true // For development only
        })
        .Services.AddHttpClient<GroqLlmSearch>(client =>
        {
            client.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "Movora-FlexiSearch/1.0");
        })
        .Services.AddHttpClient<TmdbClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.themoviedb.org/3/");
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.Add("User-Agent", "Movora-FlexiSearch/1.0");
        });

    }
}
