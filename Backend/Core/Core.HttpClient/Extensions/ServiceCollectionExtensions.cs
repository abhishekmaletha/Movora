using Core.HttpClient.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace Core.HttpClient.Extensions;

/// <summary>
/// Extension methods for configuring Core.HttpClient in DI container
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Core.HttpClient with default configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="configurationSection">The configuration section name (default: "HttpClient")</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCoreHttpClient(
        this IServiceCollection services,
        IConfiguration configuration,
        string configurationSection = HttpClientOptions.SectionName)
    {
        return services.AddCoreHttpClient(configuration, configurationSection, null);
    }

    /// <summary>
    /// Adds Core.HttpClient with custom configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="configurationSection">The configuration section name</param>
    /// <param name="configureOptions">Optional action to configure HttpClient options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCoreHttpClient(
        this IServiceCollection services,
        IConfiguration configuration,
        string configurationSection,
        Action<HttpClientOptions>? configureOptions)
    {
        // Configure HttpClient options
        var httpClientSection = configuration.GetSection(configurationSection);
        services.Configure<HttpClientOptions>(httpClientSection);

        // Apply additional configuration if provided
        if (configureOptions != null)
        {
            services.Configure<HttpClientOptions>(configureOptions);
        }

        // Get options for validation and HttpClient setup
        var options = new HttpClientOptions();
        httpClientSection.Bind(options);
        configureOptions?.Invoke(options);
        options.Validate();

        // Configure HttpClient
        var httpClientBuilder = services.AddHttpClient("CoreHttpClient", client =>
        {
            if (!string.IsNullOrEmpty(options.BaseAddress))
            {
                client.BaseAddress = new Uri(options.BaseAddress);
            }

            client.Timeout = options.Timeout;
            client.MaxResponseContentBufferSize = options.MaxResponseContentBufferSize;

            // Add default headers
            foreach (var header in options.DefaultHeaders)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }

            // Set user agent
            client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
        });

        // Configure HttpClientHandler
        httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();

            // Configure automatic decompression
            if (options.AutomaticDecompression)
            {
                handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            }

            // Configure cookies
            handler.UseCookies = options.UseCookies;

            // Configure redirects
            handler.AllowAutoRedirect = options.AllowAutoRedirect;
            handler.MaxAutomaticRedirections = options.MaxAutomaticRedirections;

            // Configure SSL
            if (options.Ssl.BypassCertificateValidation)
            {
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            }

            // Configure client certificate
            if (!string.IsNullOrEmpty(options.Ssl.ClientCertificatePath))
            {
                var clientCert = new X509Certificate2(options.Ssl.ClientCertificatePath, options.Ssl.ClientCertificatePassword);
                handler.ClientCertificates.Add(clientCert);
            }

            // Configure proxy
            if (options.Proxy != null)
            {
                var proxy = new WebProxy(options.Proxy.GetProxyUri())
                {
                    BypassProxyOnLocal = options.Proxy.BypassProxyOnLocal,
                    BypassList = options.Proxy.BypassList
                };

                if (!string.IsNullOrEmpty(options.Proxy.Username))
                {
                    proxy.Credentials = new NetworkCredential(options.Proxy.Username, options.Proxy.Password);
                }

                handler.Proxy = proxy;
                handler.UseProxy = true;
            }

            return handler;
        });

        // Note: Retry policies and circuit breakers can be added by consumers
        // using Polly or other resilience libraries as needed

        // Add logging
        if (options.EnableLogging)
        {
            httpClientBuilder.AddHttpMessageHandler<LoggingHandler>();
            services.AddTransient<LoggingHandler>();
        }

        // Register the typed HttpClient for easy injection
        services.AddTransient<System.Net.Http.HttpClient>(provider =>
        {
            var factory = provider.GetRequiredService<IHttpClientFactory>();
            return factory.CreateClient("CoreHttpClient");
        });

        return services;
    }

    /// <summary>
    /// Adds Core.HttpClient with simple configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="baseAddress">Base address for the HttpClient</param>
    /// <param name="timeoutSeconds">Request timeout in seconds</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCoreHttpClient(
        this IServiceCollection services,
        string? baseAddress = null,
        int timeoutSeconds = 30)
    {
        services.Configure<HttpClientOptions>(options =>
        {
            options.BaseAddress = baseAddress;
            options.TimeoutSeconds = timeoutSeconds;
        });

        return services.AddCoreHttpClient(options =>
        {
            options.BaseAddress = baseAddress;
            options.TimeoutSeconds = timeoutSeconds;
        });
    }

    /// <summary>
    /// Adds Core.HttpClient with custom options
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Action to configure HttpClient options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCoreHttpClient(
        this IServiceCollection services,
        Action<HttpClientOptions> configureOptions)
    {
        services.Configure<HttpClientOptions>(configureOptions);

        var options = new HttpClientOptions();
        configureOptions(options);
        options.Validate();

        var httpClientBuilder = services.AddHttpClient("CoreHttpClient", client =>
        {
            if (!string.IsNullOrEmpty(options.BaseAddress))
            {
                client.BaseAddress = new Uri(options.BaseAddress);
            }

            client.Timeout = options.Timeout;
            client.MaxResponseContentBufferSize = options.MaxResponseContentBufferSize;

            foreach (var header in options.DefaultHeaders)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }

            client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
        });

        // Note: Retry policies and circuit breakers can be added by consumers
        // using Polly or other resilience libraries as needed

        if (options.EnableLogging)
        {
            httpClientBuilder.AddHttpMessageHandler<LoggingHandler>();
            services.AddTransient<LoggingHandler>();
        }

        services.AddTransient<System.Net.Http.HttpClient>(provider =>
        {
            var factory = provider.GetRequiredService<IHttpClientFactory>();
            return factory.CreateClient("CoreHttpClient");
        });

        return services;
    }

    /// <summary>
    /// Adds a named HttpClient with Core.HttpClient extensions
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="name">The name of the HttpClient</param>
    /// <param name="configureOptions">Action to configure HttpClient options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddNamedCoreHttpClient(
        this IServiceCollection services,
        string name,
        Action<HttpClientOptions> configureOptions)
    {
        var options = new HttpClientOptions();
        configureOptions(options);
        options.Validate();

        var httpClientBuilder = services.AddHttpClient(name, client =>
        {
            if (!string.IsNullOrEmpty(options.BaseAddress))
            {
                client.BaseAddress = new Uri(options.BaseAddress);
            }

            client.Timeout = options.Timeout;
            client.MaxResponseContentBufferSize = options.MaxResponseContentBufferSize;

            foreach (var header in options.DefaultHeaders)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }

            client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
        });

        // Note: Retry policies and circuit breakers can be added by consumers
        // using Polly or other resilience libraries as needed

        if (options.EnableLogging)
        {
            httpClientBuilder.AddHttpMessageHandler<LoggingHandler>();
            services.AddTransient<LoggingHandler>();
        }

        return services;
    }


}

/// <summary>
/// Logging handler for HttpClient requests
/// </summary>
public class LoggingHandler : DelegatingHandler
{
    private readonly ILogger<LoggingHandler> _logger;

    public LoggingHandler(ILogger<LoggingHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        
        _logger.LogInformation("HTTP {Method} {Uri} - Starting request",
            request.Method, request.RequestUri);

        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("HTTP {Method} {Uri} - {StatusCode} in {Duration}ms",
                request.Method, request.RequestUri, response.StatusCode, duration.TotalMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            
            _logger.LogError(ex, "HTTP {Method} {Uri} - Failed after {Duration}ms",
                request.Method, request.RequestUri, duration.TotalMilliseconds);
            
            throw;
        }
    }
}
