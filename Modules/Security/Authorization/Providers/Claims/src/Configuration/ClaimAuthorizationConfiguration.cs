namespace RaccoonLand.Modules.Security.Authorization.Claims.Configuration;

/// <summary>
/// Configuration-bindable shape of the claim authorization rules. Bound from an <c>IConfiguration</c> section
/// and applied to <see cref="ClaimAuthorizationOptions"/>. Custom assertions are code-only and have no
/// configuration counterpart.
/// </summary>
public sealed class ClaimAuthorizationConfiguration
{
    /// <summary>Requests that are publicly accessible without authentication.</summary>
    public List<string> AnonymousRequests { get; set; } = [];

    /// <summary>Per-request policies, keyed by the request type's full name.</summary>
    public Dictionary<string, ClaimPolicyConfiguration> Policies { get; set; } = [];
}

/// <summary>Configuration-bindable shape of a single request's policy.</summary>
public sealed class ClaimPolicyConfiguration
{
    /// <summary>
    /// Claim requirements. An entry with no claims means "any authenticated caller". All listed claims must be
    /// satisfied (AND).
    /// </summary>
    public List<ClaimRequirementConfiguration> Claims { get; set; } = [];
}

/// <summary>Configuration-bindable shape of a single claim requirement.</summary>
public sealed class ClaimRequirementConfiguration
{
    /// <summary>The required claim type.</summary>
    public string ClaimType { get; set; } = string.Empty;

    /// <summary>Accepted claim values (OR). Empty means any value of <see cref="ClaimType"/> is accepted.</summary>
    public List<string> AllowedValues { get; set; } = [];
}
