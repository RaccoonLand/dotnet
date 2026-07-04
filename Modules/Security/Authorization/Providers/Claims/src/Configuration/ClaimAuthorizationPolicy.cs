using System.Security.Claims;

namespace RaccoonLand.Modules.Security.Authorization.Claims.Configuration;

/// <summary>
/// The access policy for a single request: a set of claim requirements plus optional custom assertions. The
/// policy is satisfied only when <b>every</b> requirement and <b>every</b> assertion holds (AND). An empty
/// policy (no requirements, no assertions) means "any authenticated caller".
/// </summary>
public sealed class ClaimAuthorizationPolicy
{
    /// <summary>Claim requirements; all must be satisfied (AND across requirements).</summary>
    public IList<ClaimRequirement> Requirements { get; } = [];

    /// <summary>Custom assertions over the caller's principal; all must return <see langword="true"/> (AND).</summary>
    public IList<Func<ClaimsPrincipal, bool>> Assertions { get; } = [];

    /// <summary>Returns <see langword="true"/> when the principal satisfies every requirement and assertion.</summary>
    public bool IsSatisfiedBy(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        foreach (var requirement in Requirements)
        {
            if (!requirement.IsSatisfiedBy(principal))
            {
                return false;
            }
        }

        foreach (var assertion in Assertions)
        {
            if (!assertion(principal))
            {
                return false;
            }
        }

        return true;
    }
}
