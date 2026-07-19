namespace RaccoonLand.Modules.Security.Authorization.Abstractions;

/// <summary>
/// Decides whether the current caller may execute a request. The provider is the single source of truth:
/// it determines both whether a request is publicly accessible (anonymous) and whether an authenticated
/// caller has access. Access rules are supplied to the provider from the outside (configuration, database,
/// or a remote service) — the request itself carries no authorization metadata (no attribute, no marker
/// interface).
/// </summary>
public interface IAuthorizationProvider
{
    /// <summary>
    /// Returns the authorization decision for the request described by <paramref name="context"/>.
    /// Implementations should be <b>deny-by-default</b>: a request that is not explicitly anonymous or
    /// explicitly permitted must not be allowed.
    /// Infrastructure failures (for example HTTP timeouts or database errors) should propagate as exceptions
    /// and must not be silently mapped to <see cref="AuthorizationStatus.Denied"/>.
    /// Simple in-memory implementations may ignore <paramref name="cancellationToken"/>.
    /// </summary>
    Task<AuthorizationDecision> AuthorizeAsync(
        AuthorizationContext context,
        CancellationToken cancellationToken);
}
