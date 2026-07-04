using System.Security.Claims;

namespace RaccoonLand.Modules.Security.Authorization.Claims.Configuration;

/// <summary>
/// A single claim requirement for a request: the caller must have a claim of <see cref="ClaimType"/> whose
/// value is one of <see cref="AllowedValues"/> (an OR match). When <see cref="AllowedValues"/> is empty, the
/// presence of any claim of <see cref="ClaimType"/> satisfies the requirement.
/// </summary>
public sealed record ClaimRequirement(string ClaimType, IReadOnlyList<string> AllowedValues)
{
    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="principal"/> satisfies this requirement. With no
    /// allowed values, any claim of <see cref="ClaimType"/> matches; otherwise the principal must have a claim
    /// of <see cref="ClaimType"/> whose value equals one of <see cref="AllowedValues"/> (OR).
    /// </summary>
    public bool IsSatisfiedBy(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        if (AllowedValues.Count == 0)
        {
            return principal.HasClaim(claim => claim.Type == ClaimType);
        }

        return AllowedValues.Any(value => principal.HasClaim(ClaimType, value));
    }
}
