using Microsoft.Extensions.Options;
using RaccoonLand.Modules.Security.Authorization.Abstractions;
using RaccoonLand.Modules.Security.Authorization.Claims.Configuration;
using RaccoonLand.Modules.Security.Authorization.Claims.Principals;

namespace RaccoonLand.Modules.Security.Authorization.Claims.Provider;

/// <summary>
/// An <see cref="IAuthorizationProvider"/> that decides access from <see cref="ClaimAuthorizationOptions"/>
/// against the current caller's <c>ClaimsPrincipal</c>. It returns only a status — codes and messages are the
/// middleware's responsibility. Deny-by-default:
/// <list type="number">
///   <item><description>request is anonymous → <see cref="AuthorizationStatus.Allowed"/>;</description></item>
///   <item><description>no authenticated principal → <see cref="AuthorizationStatus.Unauthenticated"/>;</description></item>
///   <item><description>a policy exists and is satisfied → <see cref="AuthorizationStatus.Allowed"/>;</description></item>
///   <item><description>otherwise → <see cref="AuthorizationStatus.Denied"/>.</description></item>
/// </list>
/// Decisions are in-memory and synchronous; <see cref="CancellationToken"/> is accepted for contract
/// compatibility and is not observed.
/// </summary>
public sealed class ClaimAuthorizationProvider(
    IClaimsPrincipalAccessor claimsPrincipalAccessor,
    IOptions<ClaimAuthorizationOptions> options) : IAuthorizationProvider
{
    private readonly ClaimAuthorizationOptions _options = options.Value;

    public Task<AuthorizationDecision> AuthorizeAsync(
        AuthorizationContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        return Task.FromResult(Authorize(context.RequestName));
    }

    private AuthorizationDecision Authorize(string requestName)
    {
        if (_options.AnonymousRequests.Contains(requestName))
        {
            return AuthorizationDecision.Allow();
        }

        var principal = claimsPrincipalAccessor.Principal;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return AuthorizationDecision.Unauthenticated();
        }

        if (!_options.Policies.TryGetValue(requestName, out var policy))
        {
            return AuthorizationDecision.Deny();
        }

        return policy.IsSatisfiedBy(principal)
            ? AuthorizationDecision.Allow()
            : AuthorizationDecision.Deny();
    }
}
