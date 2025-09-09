using Core.Authentication.Configuration;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Json;

namespace Core.Authentication.Services;

/// <summary>
/// Implementation of Keycloak user service
/// </summary>
public class KeycloakUserService : IKeycloakUserService
{
    private readonly KeycloakOptions _keycloakOptions;
    private readonly JwtValidationOptions _jwtValidationOptions;

    public KeycloakUserService(
        IOptions<KeycloakOptions> keycloakOptions,
        IOptions<JwtValidationOptions> jwtValidationOptions)
    {
        _keycloakOptions = keycloakOptions.Value;
        _jwtValidationOptions = jwtValidationOptions.Value;
    }

    /// <inheritdoc />
    public Task EnrichUserClaimsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        // Add any additional claims processing here
        // For example, you might want to add custom claims based on Keycloak roles
        var identity = principal.Identity as ClaimsIdentity;
        if (identity == null) return Task.CompletedTask;

        // Process realm roles
        ProcessRealmRoles(identity);

        // Process resource roles
        ProcessResourceRoles(identity);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public string? GetUserId(ClaimsPrincipal principal)
    {
        return principal.FindFirst("sub")?.Value ?? 
               principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    /// <inheritdoc />
    public string? GetUsername(ClaimsPrincipal principal)
    {
        return principal.FindFirst("preferred_username")?.Value ?? 
               principal.FindFirst(ClaimTypes.Name)?.Value ??
               principal.FindFirst("name")?.Value;
    }

    /// <inheritdoc />
    public string? GetEmail(ClaimsPrincipal principal)
    {
        return principal.FindFirst("email")?.Value ?? 
               principal.FindFirst(ClaimTypes.Email)?.Value;
    }

    /// <inheritdoc />
    public IEnumerable<string> GetRoles(ClaimsPrincipal principal)
    {
        var roles = new HashSet<string>();

        // Get realm roles
        var realmRolesClaim = principal.FindFirst("realm_access")?.Value;
        if (!string.IsNullOrEmpty(realmRolesClaim))
        {
            try
            {
                var realmAccess = JsonSerializer.Deserialize<JsonElement>(realmRolesClaim);
                if (realmAccess.TryGetProperty("roles", out var realmRoles) && realmRoles.ValueKind == JsonValueKind.Array)
                {
                    foreach (var role in realmRoles.EnumerateArray())
                    {
                        var roleName = role.GetString();
                        if (!string.IsNullOrEmpty(roleName))
                            roles.Add(roleName);
                    }
                }
            }
            catch
            {
                // Ignore JSON parsing errors
            }
        }

        // Get standard role claims
        foreach (var roleClaim in principal.FindAll(ClaimTypes.Role))
        {
            if (!string.IsNullOrEmpty(roleClaim.Value))
                roles.Add(roleClaim.Value);
        }

        // Get resource roles
        var resourceRolesClaim = principal.FindFirst("resource_access")?.Value;
        if (!string.IsNullOrEmpty(resourceRolesClaim))
        {
            try
            {
                var resourceAccess = JsonSerializer.Deserialize<JsonElement>(resourceRolesClaim);
                foreach (var resource in resourceAccess.EnumerateObject())
                {
                    if (resource.Value.TryGetProperty("roles", out var resourceRoles) && resourceRoles.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var role in resourceRoles.EnumerateArray())
                        {
                            var roleName = role.GetString();
                            if (!string.IsNullOrEmpty(roleName))
                                roles.Add($"{resource.Name}:{roleName}");
                        }
                    }
                }
            }
            catch
            {
                // Ignore JSON parsing errors
            }
        }

        return roles;
    }

    /// <inheritdoc />
    public bool HasRole(ClaimsPrincipal principal, string role)
    {
        return GetRoles(principal).Contains(role, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public Dictionary<string, string> GetCustomClaims(ClaimsPrincipal principal)
    {
        var customClaims = new Dictionary<string, string>();

        // Standard Keycloak claims to exclude from custom claims
        var standardClaims = new HashSet<string>
        {
            "iss", "aud", "sub", "iat", "exp", "azp", "scope", "email_verified",
            "name", "preferred_username", "given_name", "family_name", "email",
            "realm_access", "resource_access", "typ", "azp", "session_state",
            "acr", "allowed-origins", "scope", "sid", ClaimTypes.NameIdentifier,
            ClaimTypes.Name, ClaimTypes.Email, ClaimTypes.Role
        };

        foreach (var claim in principal.Claims)
        {
            if (!standardClaims.Contains(claim.Type) && !string.IsNullOrEmpty(claim.Value))
            {
                customClaims[claim.Type] = claim.Value;
            }
        }

        return customClaims;
    }

    /// <inheritdoc />
    public string? GetClaim(ClaimsPrincipal principal, string claimType)
    {
        return principal.FindFirst(claimType)?.Value;
    }

    private void ProcessRealmRoles(ClaimsIdentity identity)
    {
        var realmAccessClaim = identity.FindFirst("realm_access");
        if (realmAccessClaim?.Value == null) return;

        try
        {
            var realmAccess = JsonSerializer.Deserialize<JsonElement>(realmAccessClaim.Value);
            if (realmAccess.TryGetProperty("roles", out var roles) && roles.ValueKind == JsonValueKind.Array)
            {
                foreach (var role in roles.EnumerateArray())
                {
                    var roleName = role.GetString();
                    if (!string.IsNullOrEmpty(roleName))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                    }
                }
            }
        }
        catch
        {
            // Ignore JSON parsing errors
        }
    }

    private void ProcessResourceRoles(ClaimsIdentity identity)
    {
        var resourceAccessClaim = identity.FindFirst("resource_access");
        if (resourceAccessClaim?.Value == null) return;

        try
        {
            var resourceAccess = JsonSerializer.Deserialize<JsonElement>(resourceAccessClaim.Value);
            foreach (var resource in resourceAccess.EnumerateObject())
            {
                if (resource.Value.TryGetProperty("roles", out var roles) && roles.ValueKind == JsonValueKind.Array)
                {
                    foreach (var role in roles.EnumerateArray())
                    {
                        var roleName = role.GetString();
                        if (!string.IsNullOrEmpty(roleName))
                        {
                            // Add as both resource-specific and general role claims
                            identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                            identity.AddClaim(new Claim("resource_role", $"{resource.Name}:{roleName}"));
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore JSON parsing errors
        }
    }
}
