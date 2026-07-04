namespace RaccoonLand.Modules.Security.Authorization.Middlewares;

/// <summary>
/// Stable message template keys produced by <see cref="AuthorizationMiddleware"/> for non-allowed decisions.
/// Each key is used both as the <c>PipelineMessage.Code</c> and as the localization template key resolved
/// through <c>IMessageLocalization</c> (when available). Keys follow the framework's UPPER_SNAKE_CASE
/// convention; register translations for them in your localization store.
/// </summary>
public static class AuthorizationMessageTemplates
{
    /// <summary>No authenticated caller is available; authentication is required first (maps to 401).</summary>
    public const string AuthenticationRequired = "AUTHENTICATION_REQUIRED";

    /// <summary>The authenticated caller is not permitted to execute the request (maps to 403).</summary>
    public const string AccessDenied = "ACCESS_DENIED";
}
