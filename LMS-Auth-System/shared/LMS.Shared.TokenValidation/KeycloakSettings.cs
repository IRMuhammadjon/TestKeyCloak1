namespace LMS.Shared.TokenValidation;

public class KeycloakSettings
{
    public string AuthServerUrl { get; set; } = string.Empty;
    public string Realm { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public bool RequireHttpsMetadata { get; set; } = false;
    public bool ValidateIssuer { get; set; } = true;
    public bool ValidateAudience { get; set; } = false;
    public bool ValidateLifetime { get; set; } = true;
    public bool ValidateIssuerSigningKey { get; set; } = true;

    public string Authority => $"{AuthServerUrl}/realms/{Realm}";
    public string MetadataAddress => $"{Authority}/.well-known/openid-configuration";
}
