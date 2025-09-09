# Core.Authentication

A Keycloak authentication wrapper for .NET WebAPI projects that provides easy integration with Keycloak authentication.

## Features

- Easy integration with .NET WebAPI projects
- JWT Bearer token validation
- Keycloak realm and resource role support
- User service for extracting user information from tokens
- Authorization policy builders for role-based access control
- Configurable token validation options
- Support for custom claims processing

## Installation

Add this project as a reference to your .NET WebAPI project:

```xml
<ProjectReference Include="path/to/Core.Authentication/Core.Authentication.csproj" />
```

## Quick Start

### 1. Configuration

Add Keycloak configuration to your `appsettings.json`:

```json
{
  "Keycloak": {
    "Authority": "https://your-keycloak-server.com",
    "Realm": "your-realm",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "RequireHttpsMetadata": true,
    "Audience": "your-audience"
  }
}
```

### 2. Service Registration

In your `Program.cs`, add Keycloak authentication:

```csharp
using Core.Authentication.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Keycloak authentication
builder.Services.AddKeycloakAuthentication(builder.Configuration);

// Add authorization
builder.Services.AddAuthorization();

var app = builder.Build();

// Use authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
```

### 3. Protect Your Controllers

Use the `[Authorize]` attribute to protect your endpoints:

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WeatherForecastController : ControllerBase
{
    [HttpGet]
    public IEnumerable<WeatherForecast> Get()
    {
        // This endpoint requires authentication
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateTime.Now.AddDays(index),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        });
    }
}
```

## Advanced Usage

### Custom Configuration

You can customize the authentication configuration:

```csharp
builder.Services.AddKeycloakAuthentication(
    builder.Configuration,
    keycloakOptions =>
    {
        keycloakOptions.ClockSkew = TimeSpan.FromMinutes(2);
        keycloakOptions.SaveTokens = false;
    },
    jwtValidationOptions =>
    {
        jwtValidationOptions.NameClaimType = "preferred_username";
        jwtValidationOptions.RoleClaimType = "realm_access.roles";
    });
```

### Role-Based Authorization

Create policies for role-based access control:

```csharp
using Core.Authentication.Authorization;

builder.Services.AddAuthorization(options =>
{
    // Require admin role
    options.AddPolicy("AdminOnly", KeycloakAuthorizationPolicyBuilder.RequireRealmRoles("admin"));
    
    // Require either admin or manager role
    options.AddPolicy("AdminOrManager", KeycloakAuthorizationPolicyBuilder.RequireRealmRoles("admin", "manager"));
    
    // Require specific resource role
    options.AddPolicy("ApiAccess", KeycloakAuthorizationPolicyBuilder.RequireResourceRoles("my-api", "read"));
});
```

Use policies in your controllers:

```csharp
[HttpGet("admin")]
[Authorize(Policy = "AdminOnly")]
public IActionResult GetAdminData()
{
    return Ok("This is admin-only data");
}

[HttpPost("manage")]
[Authorize(Policy = "AdminOrManager")]
public IActionResult ManageData()
{
    return Ok("This requires admin or manager role");
}
```

### User Information Service

Inject the user service to extract user information:

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IKeycloakUserService _userService;

    public UserController(IKeycloakUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("profile")]
    public IActionResult GetProfile()
    {
        var userId = _userService.GetUserId(User);
        var username = _userService.GetUsername(User);
        var email = _userService.GetEmail(User);
        var roles = _userService.GetRoles(User);

        return Ok(new
        {
            UserId = userId,
            Username = username,
            Email = email,
            Roles = roles
        });
    }

    [HttpGet("check-role/{role}")]
    public IActionResult CheckRole(string role)
    {
        var hasRole = _userService.HasRole(User, role);
        return Ok(new { HasRole = hasRole });
    }
}
```

### Token Validation Service

Use the token validator for manual token validation:

```csharp
[ApiController]
[Route("api/[controller]")]
public class TokenController : ControllerBase
{
    private readonly IKeycloakTokenValidator _tokenValidator;

    public TokenController(IKeycloakTokenValidator tokenValidator)
    {
        _tokenValidator = tokenValidator;
    }

    [HttpPost("validate")]
    public async Task<IActionResult> ValidateToken([FromBody] string token)
    {
        var result = await _tokenValidator.ValidateTokenWithResultAsync(token);
        
        if (result.IsValid)
        {
            return Ok(new { Valid = true, Claims = result.Principal?.Claims.Select(c => new { c.Type, c.Value }) });
        }
        
        return BadRequest(new { Valid = false, Error = result.Error });
    }
}
```

## Configuration Options

### KeycloakOptions

| Property | Description | Required | Default |
|----------|-------------|----------|---------|
| Authority | Keycloak server URL | Yes | - |
| Realm | Keycloak realm name | Yes | - |
| ClientId | Application client ID | Yes | - |
| ClientSecret | Client secret (for confidential clients) | No | null |
| RequireHttpsMetadata | Require HTTPS for metadata | No | true |
| SaveTokens | Save tokens in auth properties | No | true |
| Audience | JWT audience | No | null |
| ValidIssuers | Valid token issuers | No | [] |
| ClockSkew | Clock skew tolerance | No | 5 minutes |
| MetadataAddress | Custom metadata address | No | null |

### JwtValidationOptions

| Property | Description | Default |
|----------|-------------|---------|
| ValidateIssuer | Validate token issuer | true |
| ValidateAudience | Validate token audience | true |
| ValidateLifetime | Validate token expiration | true |
| ValidateIssuerSigningKey | Validate signing key | true |
| RequireSignedTokens | Require signed tokens | true |
| RequireExpirationTime | Require expiration time | true |
| NameClaimType | Name claim type | "preferred_username" |
| RoleClaimType | Role claim type | "realm_access.roles" |

## Error Handling

The library includes built-in error handling for common scenarios:

- Invalid or expired tokens
- Network issues when fetching Keycloak metadata
- Malformed configuration

Configure custom error handling in your authentication events if needed.

## License

This project is licensed under the MIT License.
