namespace RaccoonLand.Modules.Security.Authorization.Abstractions;

/// <summary>
/// The information an <see cref="IAuthorizationProvider"/> needs to make a decision.
/// </summary>
/// <param name="RequestName">
/// The full name of the <b>concrete</b> request type (<c>request.GetType().FullName</c>). This is the key
/// providers use to look up access rules. The host pipeline must dispatch the concrete request instance;
/// proxies, decorators, or unexpected runtime types are not supported and will not match registered rules
/// (fail-closed deny). <c>FullName</c> must be non-null — the middleware throws if it is missing.
/// </param>
public sealed record AuthorizationContext(string RequestName);
