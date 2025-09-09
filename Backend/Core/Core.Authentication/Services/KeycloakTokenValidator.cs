using Core.Authentication.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Core.Authentication.Services;

/// <summary>
/// Implementation of Keycloak token validation service
/// </summary>
public class KeycloakTokenValidator : IKeycloakTokenValidator
{
    private readonly KeycloakOptions _keycloakOptions;
    private readonly JwtValidationOptions _jwtValidationOptions;
    private readonly IConfigurationManager<OpenIdConnectConfiguration> _configurationManager;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public KeycloakTokenValidator(
        IOptions<KeycloakOptions> keycloakOptions,
        IOptions<JwtValidationOptions> jwtValidationOptions)
    {
        _keycloakOptions = keycloakOptions.Value;
        _jwtValidationOptions = jwtValidationOptions.Value;
        _tokenHandler = new JwtSecurityTokenHandler();

        _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            _keycloakOptions.GetMetadataAddress(),
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever());
    }

    /// <inheritdoc />
    public async Task<ClaimsPrincipal?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await ValidateTokenWithResultAsync(token, cancellationToken);
            return result.IsValid ? result.Principal : null;
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<TokenValidationResult> ValidateTokenWithResultAsync(string token, CancellationToken cancellationToken = default)
    {
        var result = new TokenValidationResult();

        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                result.Error = "Token is null or empty";
                return result;
            }

            // Get OpenID Connect configuration
            var configuration = await _configurationManager.GetConfigurationAsync(cancellationToken);

            // Create token validation parameters
            var validationParameters = _jwtValidationOptions.ToTokenValidationParameters(_keycloakOptions);
            validationParameters.IssuerSigningKeys = configuration.SigningKeys;

            // Validate token
            var validationResult = await _tokenHandler.ValidateTokenAsync(token, validationParameters);

            if (validationResult.IsValid)
            {
                result.IsValid = true;
                result.Principal = new ClaimsPrincipal(validationResult.ClaimsIdentity);
            }
            else
            {
                result.Error = validationResult.Exception?.Message ?? "Token validation failed";
                result.Exception = validationResult.Exception;
            }
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
            result.Exception = ex;
        }

        return result;
    }

    /// <inheritdoc />
    public IEnumerable<Claim> ExtractClaims(string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
                return Enumerable.Empty<Claim>();

            var jwtToken = _tokenHandler.ReadJwtToken(token);
            return jwtToken.Claims;
        }
        catch
        {
            return Enumerable.Empty<Claim>();
        }
    }
}
