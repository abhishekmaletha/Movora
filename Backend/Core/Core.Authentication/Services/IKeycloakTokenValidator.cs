using System.Security.Claims;

namespace Core.Authentication.Services;

/// <summary>
/// Interface for Keycloak token validation services
/// </summary>
public interface IKeycloakTokenValidator
{
    /// <summary>
    /// Validates a JWT token from Keycloak
    /// </summary>
    /// <param name="token">The JWT token to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ClaimsPrincipal if validation is successful, null otherwise</returns>
    Task<ClaimsPrincipal?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a JWT token and returns detailed validation result
    /// </summary>
    /// <param name="token">The JWT token to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Token validation result</returns>
    Task<TokenValidationResult> ValidateTokenWithResultAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts claims from a validated token without full validation
    /// </summary>
    /// <param name="token">The JWT token</param>
    /// <returns>Claims from the token</returns>
    IEnumerable<Claim> ExtractClaims(string token);
}

/// <summary>
/// Token validation result
/// </summary>
public class TokenValidationResult
{
    /// <summary>
    /// Whether the token is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// The validated claims principal
    /// </summary>
    public ClaimsPrincipal? Principal { get; set; }

    /// <summary>
    /// Validation error message if any
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Exception that occurred during validation
    /// </summary>
    public Exception? Exception { get; set; }
}
