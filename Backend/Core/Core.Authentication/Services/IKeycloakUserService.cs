using System.Security.Claims;

namespace Core.Authentication.Services;

/// <summary>
/// Interface for Keycloak user-related services
/// </summary>
public interface IKeycloakUserService
{
    /// <summary>
    /// Enriches the user's claims with additional information from Keycloak token
    /// </summary>
    /// <param name="principal">The current claims principal</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task EnrichUserClaimsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the user ID from the claims principal
    /// </summary>
    /// <param name="principal">The claims principal</param>
    /// <returns>User ID if found</returns>
    string? GetUserId(ClaimsPrincipal principal);

    /// <summary>
    /// Gets the username from the claims principal
    /// </summary>
    /// <param name="principal">The claims principal</param>
    /// <returns>Username if found</returns>
    string? GetUsername(ClaimsPrincipal principal);

    /// <summary>
    /// Gets the email from the claims principal
    /// </summary>
    /// <param name="principal">The claims principal</param>
    /// <returns>Email if found</returns>
    string? GetEmail(ClaimsPrincipal principal);

    /// <summary>
    /// Gets the user's roles from the claims principal
    /// </summary>
    /// <param name="principal">The claims principal</param>
    /// <returns>List of user roles</returns>
    IEnumerable<string> GetRoles(ClaimsPrincipal principal);

    /// <summary>
    /// Checks if the user has a specific role
    /// </summary>
    /// <param name="principal">The claims principal</param>
    /// <param name="role">The role to check</param>
    /// <returns>True if user has the role</returns>
    bool HasRole(ClaimsPrincipal principal, string role);

    /// <summary>
    /// Gets all custom claims from the token
    /// </summary>
    /// <param name="principal">The claims principal</param>
    /// <returns>Dictionary of custom claims</returns>
    Dictionary<string, string> GetCustomClaims(ClaimsPrincipal principal);

    /// <summary>
    /// Gets a specific claim value
    /// </summary>
    /// <param name="principal">The claims principal</param>
    /// <param name="claimType">The claim type to retrieve</param>
    /// <returns>Claim value if found</returns>
    string? GetClaim(ClaimsPrincipal principal, string claimType);
}
