namespace Core.Authentication.Configuration;

/// <summary>
/// Configuration options for Keycloak authentication
/// </summary>
public class KeycloakOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "Keycloak";

    /// <summary>
    /// Keycloak server URL (e.g., https://auth.example.com)
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// Keycloak realm name
    /// </summary>
    public string Realm { get; set; } = string.Empty;

    /// <summary>
    /// Client ID for the application
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client secret (optional, for confidential clients)
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Whether to require HTTPS metadata
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>
    /// Whether to save tokens in authentication properties
    /// </summary>
    public bool SaveTokens { get; set; } = true;

    /// <summary>
    /// Audience for JWT token validation
    /// </summary>
    public string? Audience { get; set; }

    /// <summary>
    /// Valid issuers for JWT token validation
    /// </summary>
    public List<string> ValidIssuers { get; set; } = new();

    /// <summary>
    /// Clock skew tolerance for token validation
    /// </summary>
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Metadata address override (if different from standard Keycloak discovery endpoint)
    /// </summary>
    public string? MetadataAddress { get; set; }

    /// <summary>
    /// Gets the full authority URL including realm
    /// </summary>
    public string GetRealmAuthority() => $"{Authority.TrimEnd('/')}/realms/{Realm}";

    /// <summary>
    /// Gets the OpenID Connect metadata URL
    /// </summary>
    public string GetMetadataAddress() => 
        MetadataAddress ?? $"{GetRealmAuthority()}/.well-known/openid_configuration";

    /// <summary>
    /// Validates the configuration
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Authority))
            throw new ArgumentException("Keycloak Authority is required", nameof(Authority));

        if (string.IsNullOrWhiteSpace(Realm))
            throw new ArgumentException("Keycloak Realm is required", nameof(Realm));

        if (string.IsNullOrWhiteSpace(ClientId))
            throw new ArgumentException("Keycloak ClientId is required", nameof(ClientId));

        if (!Uri.IsWellFormedUriString(Authority, UriKind.Absolute))
            throw new ArgumentException("Keycloak Authority must be a valid absolute URI", nameof(Authority));
    }
}
