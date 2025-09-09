using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Core.Authentication.Authorization;

/// <summary>
/// Builder for creating Keycloak-specific authorization policies
/// </summary>
public static class KeycloakAuthorizationPolicyBuilder
{
    /// <summary>
    /// Creates a policy that requires the user to have at least one of the specified realm roles
    /// </summary>
    /// <param name="roles">The realm roles required</param>
    /// <returns>Authorization policy</returns>
    public static AuthorizationPolicy RequireRealmRoles(params string[] roles)
    {
        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireAssertion(context =>
            {
                var userRoles = GetRealmRoles(context.User);
                return roles.Any(role => userRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
            })
            .Build();
    }

    /// <summary>
    /// Creates a policy that requires the user to have all of the specified realm roles
    /// </summary>
    /// <param name="roles">The realm roles required</param>
    /// <returns>Authorization policy</returns>
    public static AuthorizationPolicy RequireAllRealmRoles(params string[] roles)
    {
        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireAssertion(context =>
            {
                var userRoles = GetRealmRoles(context.User);
                return roles.All(role => userRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
            })
            .Build();
    }

    /// <summary>
    /// Creates a policy that requires the user to have at least one of the specified resource roles
    /// </summary>
    /// <param name="resource">The resource name</param>
    /// <param name="roles">The resource roles required</param>
    /// <returns>Authorization policy</returns>
    public static AuthorizationPolicy RequireResourceRoles(string resource, params string[] roles)
    {
        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireAssertion(context =>
            {
                var userResourceRoles = GetResourceRoles(context.User, resource);
                return roles.Any(role => userResourceRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
            })
            .Build();
    }

    /// <summary>
    /// Creates a policy that requires the user to have all of the specified resource roles
    /// </summary>
    /// <param name="resource">The resource name</param>
    /// <param name="roles">The resource roles required</param>
    /// <returns>Authorization policy</returns>
    public static AuthorizationPolicy RequireAllResourceRoles(string resource, params string[] roles)
    {
        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireAssertion(context =>
            {
                var userResourceRoles = GetResourceRoles(context.User, resource);
                return roles.All(role => userResourceRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
            })
            .Build();
    }

    /// <summary>
    /// Creates a policy that requires a specific claim with a specific value
    /// </summary>
    /// <param name="claimType">The claim type</param>
    /// <param name="claimValue">The claim value</param>
    /// <returns>Authorization policy</returns>
    public static AuthorizationPolicy RequireClaim(string claimType, string claimValue)
    {
        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireClaim(claimType, claimValue)
            .Build();
    }

    /// <summary>
    /// Creates a policy that requires a specific claim to exist (any value)
    /// </summary>
    /// <param name="claimType">The claim type</param>
    /// <returns>Authorization policy</returns>
    public static AuthorizationPolicy RequireClaim(string claimType)
    {
        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireClaim(claimType)
            .Build();
    }

    /// <summary>
    /// Creates a custom policy with a custom requirement
    /// </summary>
    /// <param name="requirement">Custom authorization requirement</param>
    /// <returns>Authorization policy</returns>
    public static AuthorizationPolicy RequireCustom(Func<ClaimsPrincipal, bool> requirement)
    {
        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireAssertion(context => requirement(context.User))
            .Build();
    }

    private static IEnumerable<string> GetRealmRoles(ClaimsPrincipal principal)
    {
        return principal.FindAll(ClaimTypes.Role).Select(c => c.Value);
    }

    private static IEnumerable<string> GetResourceRoles(ClaimsPrincipal principal, string resource)
    {
        return principal.FindAll("resource_role")
            .Select(c => c.Value)
            .Where(v => v.StartsWith($"{resource}:", StringComparison.OrdinalIgnoreCase))
            .Select(v => v.Substring(resource.Length + 1));
    }
}
