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
    /// When <see langword="true"/>, sets <c>MapInboundClaims = false</c> on schemes registered by this package
    /// so inbound JWT claim types remain provider-native (for example <c>sub</c> instead of the legacy SOAP URI).
    /// Does not mutate process-wide static claim type maps; see <see cref="ClearGlobalJwtClaimTypeMaps"/>.
    /// </summary>
    public bool DisableDefaultClaimMapping { get; set; } = true;

    /// <summary>
    /// When <see langword="true"/>, clears Microsoft's process-wide JWT claim type maps
    /// (<c>JwtSecurityTokenHandler</c> / <c>JsonWebTokenHandler</c> defaults).
    /// Opt-in only: the change is irreversible for the process lifetime and affects other JWT handlers in the process
    /// (including schemes registered outside this package). Prefer <see cref="DisableDefaultClaimMapping"/> for
    /// scheme-local behaviour.
    /// </summary>
    public bool ClearGlobalJwtClaimTypeMaps { get; set; }

    /// <summary>
    /// When <see langword="false"/> (default), each set <c>Default*</c> scheme must name a JwtBearer or OpenIdConnect
    /// scheme registered by this call (typo protection for API/JWT apps).
    /// When <see langword="true"/>, defaults may name schemes registered elsewhere (for example Cookie + OIDC).
    /// Existence of an external scheme in DI is not verified at registration time; a typo surfaces when authentication runs.
    /// Empty or whitespace defaults are always rejected.
    /// </summary>
    public bool AllowExternalDefaultSchemes { get; set; }

    /// <summary>
    /// JWT Bearer schemes keyed by scheme name (for example <c>Bearer</c>).
    /// This package deliberately treats keys as case-insensitive
    /// (<see cref="StringComparer.OrdinalIgnoreCase"/>) to reject ambiguous names;
    /// ASP.NET Core's scheme provider may still store names with ordinal comparison.
    /// </summary>
    public Dictionary<string, JwtBearerOptions> JwtBearer { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// OpenID Connect schemes keyed by scheme name (for example <c>oidc</c>).
    /// This package deliberately treats keys as case-insensitive
    /// (<see cref="StringComparer.OrdinalIgnoreCase"/>) to reject ambiguous names;
    /// ASP.NET Core's scheme provider may still store names with ordinal comparison.
    /// </summary>
    public Dictionary<string, OpenIdConnectOptions> OpenIdConnect { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
