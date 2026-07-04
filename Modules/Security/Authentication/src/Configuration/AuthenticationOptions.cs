namespace RaccoonLand.Modules.Security.Authentication.Configuration;

/// <summary>
/// Root authentication settings bound from the <c>Authentication</c> configuration section.
/// </summary>
public sealed class AuthenticationOptions
{
    /// <summary>Default root configuration section name (<c>Authentication</c>).</summary>
    public const string SectionName = "Authentication";

    /// <summary>Default authentication scheme for <see cref="Microsoft.AspNetCore.Authentication.AuthenticationOptions"/>.</summary>
    public string? DefaultScheme { get; set; }

    /// <summary>Default scheme used by <c>HttpContext.User</c> authentication.</summary>
    public string? DefaultAuthenticateScheme { get; set; }

    /// <summary>Default scheme used when an endpoint requires authentication.</summary>
    public string? DefaultChallengeScheme { get; set; }

    /// <summary>Default scheme used for sign-in operations.</summary>
    public string? DefaultSignInScheme { get; set; }

    /// <summary>Default scheme used for sign-out operations.</summary>
    public string? DefaultSignOutScheme { get; set; }

    /// <summary>
    /// When <see langword="true"/>, clears Microsoft's default JWT claim type maps and sets
    /// <c>MapInboundClaims = false</c> on registered schemes so claim types remain provider-native
    /// (for example <c>sub</c> instead of <c>http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier</c>).
    /// </summary>
    public bool DisableDefaultClaimMapping { get; set; } = true;

    /// <summary>JWT Bearer schemes keyed by scheme name (for example <c>Bearer</c>).</summary>
    public Dictionary<string, JwtBearerOptions> JwtBearer { get; set; } = new(StringComparer.Ordinal);

    /// <summary>OpenID Connect schemes keyed by scheme name (for example <c>oidc</c>).</summary>
    public Dictionary<string, OpenIdConnectOptions> OpenIdConnect { get; set; } = new(StringComparer.Ordinal);
}
