using System.Security.Claims;

namespace RaccoonLand.Modules.Security.Authorization.Claims.Principals;

/// <summary>
/// Supplies the current caller's <see cref="ClaimsPrincipal"/> to the claim-based authorization provider.
/// Only the claim provider needs this; other providers (for example database- or API-backed) rely on
/// <c>ICurrentExecutionContext</c> instead.
/// </summary>
public interface IClaimsPrincipalAccessor
{
    /// <summary>The current principal, or <see langword="null"/> when none is available.</summary>
    ClaimsPrincipal? Principal { get; }
}
