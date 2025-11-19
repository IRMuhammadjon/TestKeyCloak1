using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace LMS.Shared.TokenValidation;

public static class TokenValidationExtensions
{
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var keycloakSettings = new KeycloakSettings();
        configuration.GetSection("Keycloak").Bind(keycloakSettings);

        services.Configure<KeycloakSettings>(configuration.GetSection("Keycloak"));

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.Authority = keycloakSettings.Authority;
            options.MetadataAddress = keycloakSettings.MetadataAddress;
            options.RequireHttpsMetadata = keycloakSettings.RequireHttpsMetadata;
            options.Audience = keycloakSettings.ClientId;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = keycloakSettings.ValidateIssuer,
                ValidIssuer = keycloakSettings.Authority,
                ValidateAudience = keycloakSettings.ValidateAudience,
                ValidAudience = keycloakSettings.ClientId,
                ValidateLifetime = keycloakSettings.ValidateLifetime,
                ValidateIssuerSigningKey = keycloakSettings.ValidateIssuerSigningKey,
                ClockSkew = TimeSpan.FromMinutes(5),
                RoleClaimType = ClaimTypes.Role,
                NameClaimType = "preferred_username"
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var claimsIdentity = context.Principal?.Identity as ClaimsIdentity;

                    if (claimsIdentity != null)
                    {
                        // Map Keycloak roles to ClaimTypes.Role
                        var realmAccessClaim = claimsIdentity.FindFirst("realm_access");
                        if (realmAccessClaim != null)
                        {
                            var rolesJson = System.Text.Json.JsonDocument.Parse(realmAccessClaim.Value);
                            if (rolesJson.RootElement.TryGetProperty("roles", out var rolesElement))
                            {
                                foreach (var role in rolesElement.EnumerateArray())
                                {
                                    var roleValue = role.GetString();
                                    if (!string.IsNullOrEmpty(roleValue))
                                    {
                                        claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, roleValue));
                                    }
                                }
                            }
                        }
                    }

                    return Task.CompletedTask;
                },
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;

                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization();

        return services;
    }

    public static string? GetUserId(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
               user.FindFirst("sub")?.Value;
    }

    public static string? GetUsername(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Name)?.Value ??
               user.FindFirst("preferred_username")?.Value;
    }

    public static string? GetEmail(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Email)?.Value ??
               user.FindFirst("email")?.Value;
    }

    public static string? GetFirstName(this ClaimsPrincipal user)
    {
        return user.FindFirst("first_name")?.Value ??
               user.FindFirst(ClaimTypes.GivenName)?.Value;
    }

    public static string? GetLastName(this ClaimsPrincipal user)
    {
        return user.FindFirst("last_name")?.Value ??
               user.FindFirst(ClaimTypes.Surname)?.Value;
    }

    public static IEnumerable<string> GetRoles(this ClaimsPrincipal user)
    {
        return user.FindAll(ClaimTypes.Role).Select(c => c.Value);
    }

    public static bool HasRole(this ClaimsPrincipal user, string role)
    {
        return user.IsInRole(role);
    }

    public static bool HasAnyRole(this ClaimsPrincipal user, params string[] roles)
    {
        return roles.Any(role => user.IsInRole(role));
    }

    public static bool HasAllRoles(this ClaimsPrincipal user, params string[] roles)
    {
        return roles.All(role => user.IsInRole(role));
    }
}
