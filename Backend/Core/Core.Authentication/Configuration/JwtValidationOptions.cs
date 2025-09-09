using Microsoft.IdentityModel.Tokens;

namespace Core.Authentication.Configuration;

/// <summary>
/// JWT token validation configuration options
/// </summary>
public class JwtValidationOptions
{
    /// <summary>
    /// Whether to validate the issuer
    /// </summary>
    public bool ValidateIssuer { get; set; } = true;

    /// <summary>
    /// Whether to validate the audience
    /// </summary>
    public bool ValidateAudience { get; set; } = true;

    /// <summary>
    /// Whether to validate the lifetime
    /// </summary>
    public bool ValidateLifetime { get; set; } = true;

    /// <summary>
    /// Whether to validate the issuer signing key
    /// </summary>
    public bool ValidateIssuerSigningKey { get; set; } = true;

    /// <summary>
    /// Whether to require a signed token
    /// </summary>
    public bool RequireSignedTokens { get; set; } = true;

    /// <summary>
    /// Whether to require an expiration time
    /// </summary>
    public bool RequireExpirationTime { get; set; } = true;

    /// <summary>
    /// Name claim type mapping
    /// </summary>
    public string NameClaimType { get; set; } = "preferred_username";

    /// <summary>
    /// Role claim type mapping
    /// </summary>
    public string RoleClaimType { get; set; } = "realm_access.roles";

    /// <summary>
    /// Creates TokenValidationParameters from these options
    /// </summary>
    public TokenValidationParameters ToTokenValidationParameters(KeycloakOptions keycloakOptions)
    {
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = ValidateIssuer,
            ValidateAudience = ValidateAudience,
            ValidateLifetime = ValidateLifetime,
            ValidateIssuerSigningKey = ValidateIssuerSigningKey,
            RequireSignedTokens = RequireSignedTokens,
            RequireExpirationTime = RequireExpirationTime,
            ClockSkew = keycloakOptions.ClockSkew,
            NameClaimType = NameClaimType,
            RoleClaimType = RoleClaimType
        };

        // Set valid issuers
        if (keycloakOptions.ValidIssuers.Any())
        {
            validationParameters.ValidIssuers = keycloakOptions.ValidIssuers;
        }
        else
        {
            validationParameters.ValidIssuer = keycloakOptions.GetRealmAuthority();
        }

        // Set valid audience
        if (!string.IsNullOrWhiteSpace(keycloakOptions.Audience))
        {
            validationParameters.ValidAudience = keycloakOptions.Audience;
        }
        else if (!string.IsNullOrWhiteSpace(keycloakOptions.ClientId))
        {
            validationParameters.ValidAudience = keycloakOptions.ClientId;
        }

        return validationParameters;
    }
}
