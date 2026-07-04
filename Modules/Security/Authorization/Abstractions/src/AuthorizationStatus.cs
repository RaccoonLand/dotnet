namespace RaccoonLand.Modules.Security.Authorization.Abstractions;

/// <summary>
/// The outcome category of an authorization decision.
/// </summary>
public enum AuthorizationStatus
{
    /// <summary>The caller may execute the request.</summary>
    Allowed,

    /// <summary>The caller is authenticated but is not permitted to execute the request.</summary>
    Denied,

    /// <summary>No authenticated caller is available; authentication is required first.</summary>
    Unauthenticated,
}
