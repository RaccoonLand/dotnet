namespace RaccoonLand.Modules.Security.Authorization.Abstractions;

/// <summary>
/// The information an <see cref="IAuthorizationProvider"/> needs to make a decision.
/// </summary>
/// <param name="RequestName">
/// The full name of the request type (<c>request.GetType().FullName</c>), which is unique within the
/// solution and is the key providers use to look up access rules.
/// </param>
public sealed record AuthorizationContext(string RequestName);
