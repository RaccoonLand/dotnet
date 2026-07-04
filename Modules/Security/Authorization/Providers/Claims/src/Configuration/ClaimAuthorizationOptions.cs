using System.Security.Claims;

namespace RaccoonLand.Modules.Security.Authorization.Claims.Configuration;

/// <summary>
/// Externally supplied rules for <see cref="RaccoonLand.Modules.Security.Authorization.Claims.Provider.ClaimAuthorizationProvider"/>, keyed by the request type's full
/// name. A request is authorized only when it is explicitly anonymous or has an explicit policy that the
/// caller satisfies — anything else is denied (deny-by-default).
/// </summary>
public sealed class ClaimAuthorizationOptions
{
    /// <summary>Requests that are publicly accessible without authentication.</summary>
    public ISet<string> AnonymousRequests { get; } = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>
    /// Per-request policies. An empty policy means "any authenticated caller". A request with no entry (and
    /// not in <see cref="AnonymousRequests"/>) is denied.
    /// </summary>
    public IDictionary<string, ClaimAuthorizationPolicy> Policies { get; } =
        new Dictionary<string, ClaimAuthorizationPolicy>(StringComparer.Ordinal);

    /// <summary>Marks a request (by full type name) as publicly accessible.</summary>
    public ClaimAuthorizationOptions AllowAnonymous(string requestFullName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestFullName);

        AnonymousRequests.Add(requestFullName);
        return this;
    }

    /// <summary>Requires that the caller is authenticated, with no specific claim.</summary>
    public ClaimAuthorizationOptions RequireAuthenticated(string requestFullName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestFullName);

        GetOrAddPolicy(requestFullName);
        return this;
    }

    /// <summary>
    /// Requires that the caller has a claim of <paramref name="claimType"/>. When
    /// <paramref name="allowedValues"/> is empty, any value satisfies it; otherwise the claim value must match
    /// one of the supplied values (OR). Multiple calls add multiple requirements that must all hold (AND).
    /// </summary>
    public ClaimAuthorizationOptions RequireClaim(
        string requestFullName,
        string claimType,
        params string[] allowedValues)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestFullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(claimType);

        GetOrAddPolicy(requestFullName).Requirements.Add(new ClaimRequirement(claimType, allowedValues ?? []));
        return this;
    }

    /// <summary>
    /// Adds a custom assertion over the caller's <see cref="ClaimsPrincipal"/> for the request. The assertion
    /// runs only after authentication has succeeded; returning <see langword="false"/> denies access. Multiple
    /// assertions (and any claim requirements) must all hold (AND). Assertions are code-only — they cannot be
    /// supplied from configuration.
    /// </summary>
    public ClaimAuthorizationOptions RequireAssertion(
        string requestFullName,
        Func<ClaimsPrincipal, bool> assertion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestFullName);
        ArgumentNullException.ThrowIfNull(assertion);

        GetOrAddPolicy(requestFullName).Assertions.Add(assertion);
        return this;
    }

    private ClaimAuthorizationPolicy GetOrAddPolicy(string requestFullName)
    {
        if (!Policies.TryGetValue(requestFullName, out var policy))
        {
            policy = new ClaimAuthorizationPolicy();
            Policies[requestFullName] = policy;
        }

        return policy;
    }
}
