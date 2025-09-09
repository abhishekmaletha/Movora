using System.Net;

namespace Core.HttpClient.Configuration;

/// <summary>
/// Configuration options for HttpClient setup
/// </summary>
public class HttpClientOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "HttpClient";

    /// <summary>
    /// Base address for the HttpClient
    /// </summary>
    public string? BaseAddress { get; set; }

    /// <summary>
    /// Default request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Retry delay in milliseconds
    /// </summary>
    public int RetryDelayMilliseconds { get; set; } = 1000;

    /// <summary>
    /// Whether to use exponential backoff for retries
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;

    /// <summary>
    /// Whether to enable detailed logging
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Default headers to include in all requests
    /// </summary>
    public Dictionary<string, string> DefaultHeaders { get; set; } = new();

    /// <summary>
    /// User agent string
    /// </summary>
    public string UserAgent { get; set; } = "Core.HttpClient/1.0";

    /// <summary>
    /// Whether to automatically decompress responses
    /// </summary>
    public bool AutomaticDecompression { get; set; } = true;

    /// <summary>
    /// Maximum response content buffer size in bytes
    /// </summary>
    public long MaxResponseContentBufferSize { get; set; } = 10 * 1024 * 1024; // 10MB

    /// <summary>
    /// Whether to use cookies
    /// </summary>
    public bool UseCookies { get; set; } = true;

    /// <summary>
    /// Whether to follow redirects automatically
    /// </summary>
    public bool AllowAutoRedirect { get; set; } = true;

    /// <summary>
    /// Maximum number of automatic redirects
    /// </summary>
    public int MaxAutomaticRedirections { get; set; } = 10;

    /// <summary>
    /// SSL/TLS options
    /// </summary>
    public SslOptions Ssl { get; set; } = new();

    /// <summary>
    /// Proxy configuration
    /// </summary>
    public ProxyOptions? Proxy { get; set; }

    /// <summary>
    /// Circuit breaker options
    /// </summary>
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();

    /// <summary>
    /// Gets the timeout as a TimeSpan
    /// </summary>
    public TimeSpan Timeout => TimeSpan.FromSeconds(TimeoutSeconds);

    /// <summary>
    /// Gets the retry delay as a TimeSpan
    /// </summary>
    public TimeSpan RetryDelay => TimeSpan.FromMilliseconds(RetryDelayMilliseconds);

    /// <summary>
    /// Validates the configuration
    /// </summary>
    public void Validate()
    {
        if (TimeoutSeconds <= 0)
            throw new ArgumentException("TimeoutSeconds must be greater than 0", nameof(TimeoutSeconds));

        if (MaxRetryAttempts < 0)
            throw new ArgumentException("MaxRetryAttempts must be greater than or equal to 0", nameof(MaxRetryAttempts));

        if (RetryDelayMilliseconds < 0)
            throw new ArgumentException("RetryDelayMilliseconds must be greater than or equal to 0", nameof(RetryDelayMilliseconds));

        if (MaxResponseContentBufferSize <= 0)
            throw new ArgumentException("MaxResponseContentBufferSize must be greater than 0", nameof(MaxResponseContentBufferSize));

        if (MaxAutomaticRedirections < 0)
            throw new ArgumentException("MaxAutomaticRedirections must be greater than or equal to 0", nameof(MaxAutomaticRedirections));

        if (!string.IsNullOrEmpty(BaseAddress) && !Uri.IsWellFormedUriString(BaseAddress, UriKind.Absolute))
            throw new ArgumentException("BaseAddress must be a valid absolute URI", nameof(BaseAddress));

        Ssl.Validate();
        Proxy?.Validate();
        CircuitBreaker.Validate();
    }
}

/// <summary>
/// SSL/TLS configuration options
/// </summary>
public class SslOptions
{
    /// <summary>
    /// Whether to bypass SSL certificate validation (not recommended for production)
    /// </summary>
    public bool BypassCertificateValidation { get; set; } = false;

    /// <summary>
    /// Client certificate path (if required)
    /// </summary>
    public string? ClientCertificatePath { get; set; }

    /// <summary>
    /// Client certificate password
    /// </summary>
    public string? ClientCertificatePassword { get; set; }

    /// <summary>
    /// Allowed SSL protocols
    /// </summary>
    public string[] AllowedProtocols { get; set; } = { "Tls12", "Tls13" };

    /// <summary>
    /// Validates the SSL configuration
    /// </summary>
    public void Validate()
    {
        if (!string.IsNullOrEmpty(ClientCertificatePath) && !File.Exists(ClientCertificatePath))
            throw new ArgumentException("Client certificate file does not exist", nameof(ClientCertificatePath));
    }
}

/// <summary>
/// Proxy configuration options
/// </summary>
public class ProxyOptions
{
    /// <summary>
    /// Proxy server address
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Proxy port
    /// </summary>
    public int Port { get; set; } = 8080;

    /// <summary>
    /// Proxy username (if authentication is required)
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Proxy password (if authentication is required)
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Whether to bypass proxy for local addresses
    /// </summary>
    public bool BypassProxyOnLocal { get; set; } = true;

    /// <summary>
    /// Addresses to bypass proxy for
    /// </summary>
    public string[] BypassList { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets the proxy URI
    /// </summary>
    public string GetProxyUri() => $"http://{Address}:{Port}";

    /// <summary>
    /// Validates the proxy configuration
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Address))
            throw new ArgumentException("Proxy address is required", nameof(Address));

        if (Port <= 0 || Port > 65535)
            throw new ArgumentException("Proxy port must be between 1 and 65535", nameof(Port));
    }
}

/// <summary>
/// Circuit breaker configuration options
/// </summary>
public class CircuitBreakerOptions
{
    /// <summary>
    /// Whether circuit breaker is enabled
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Number of consecutive failures to trigger circuit breaker
    /// </summary>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// Duration to keep circuit open in seconds
    /// </summary>
    public int DurationOfBreakSeconds { get; set; } = 30;

    /// <summary>
    /// Number of requests allowed in half-open state
    /// </summary>
    public int SamplingDuration { get; set; } = 10;

    /// <summary>
    /// Gets the duration of break as TimeSpan
    /// </summary>
    public TimeSpan DurationOfBreak => TimeSpan.FromSeconds(DurationOfBreakSeconds);

    /// <summary>
    /// Validates the circuit breaker configuration
    /// </summary>
    public void Validate()
    {
        if (FailureThreshold <= 0)
            throw new ArgumentException("FailureThreshold must be greater than 0", nameof(FailureThreshold));

        if (DurationOfBreakSeconds <= 0)
            throw new ArgumentException("DurationOfBreakSeconds must be greater than 0", nameof(DurationOfBreakSeconds));

        if (SamplingDuration <= 0)
            throw new ArgumentException("SamplingDuration must be greater than 0", nameof(SamplingDuration));
    }
}
