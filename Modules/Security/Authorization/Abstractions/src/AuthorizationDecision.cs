namespace RaccoonLand.Modules.Security.Authorization.Abstractions;

/// <summary>
/// The result of an authorization check. A provider returns only the <see cref="Status"/> — it does not
/// produce any user-facing code or message. Turning a non-allowed decision into a response (codes, localized
/// text, status hints) is the caller's responsibility; in the RaccoonLand pipeline that caller is the
/// authorization middleware.
/// </summary>
public sealed record AuthorizationDecision
{
    /// <summary>The outcome category.</summary>
    public required AuthorizationStatus Status { get; init; }

    /// <summary>Indicates whether the request may proceed.</summary>
    public bool IsAllowed => Status == AuthorizationStatus.Allowed;

    /// <summary>The caller may execute the request.</summary>
    public static AuthorizationDecision Allow() =>
        new() { Status = AuthorizationStatus.Allowed };

    /// <summary>The caller is authenticated but not permitted to execute the request.</summary>
    public static AuthorizationDecision Deny() =>
        new() { Status = AuthorizationStatus.Denied };

    /// <summary>No authenticated caller is available; authentication is required first.</summary>
    public static AuthorizationDecision Unauthenticated() =>
        new() { Status = AuthorizationStatus.Unauthenticated };
}
