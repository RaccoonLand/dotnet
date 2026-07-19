using System.ComponentModel.DataAnnotations;

namespace RaccoonLand.Modules.Security.Authorization.Api.Configuration;

/// <summary>How the provider authenticates its service-to-service calls to the authorization API.</summary>
public enum AuthorizationApiAuthenticationMode
{
    /// <summary>No credentials are attached (for example an internal network secured at the infrastructure level).</summary>
    None = 0,

    /// <summary>An API key is sent in a configurable header (<see cref="ApiAuthorizationOptions.ApiKeyHeaderName"/>).</summary>
    ApiKey = 1,

    /// <summary>A static bearer token is sent in the <c>Authorization</c> header.</summary>
    Bearer = 2,
}

/// <summary>
/// Settings for the API authorization provider, typically bound from a configuration section such as
/// <c>appsettings.json</c>. The provider calls an external authorization (policy) API for the set of anonymous
/// request names and the set of request names the current user may execute.
/// </summary>
/// <example>
/// <code>
/// "Authorization": {
///   "Api": {
///     "BaseAddress": "https://policy.internal/api/",
///     "AnonymousRequestsPath": "anonymous-requests",
///     "AllowedRequestsPath": "users/{userId}/allowed-requests",
///     "AuthenticationMode": "ApiKey",
///     "ApiKeyHeaderName": "X-Api-Key",
///     "ApiKey": "…",
///     "EnableCache": true,
///     "UserCacheDuration": "00:01:00"
///   }
/// }
/// </code>
/// </example>
public sealed class ApiAuthorizationOptions
{
    /// <summary>Default configuration section name (<c>Authorization:Api</c>).</summary>
    public const string SectionName = "Authorization:Api";

    /// <summary>
    /// Base address of the authorization API. Should end with a trailing <c>/</c> so the relative paths below
    /// resolve correctly (for example <c>https://policy.internal/api/</c>).
    /// </summary>
    [Required]
    public Uri? BaseAddress { get; set; }

    /// <summary>
    /// Relative path of the endpoint returning the anonymous request names. Defaults to
    /// <c>anonymous-requests</c>. The response body is <c>{ "requests": [ … ] }</c>.
    /// </summary>
    public string AnonymousRequestsPath { get; set; } = "anonymous-requests";

    /// <summary>
    /// Relative path of the endpoint returning the request names allowed for a user. Must contain the
    /// <c>{userId}</c> placeholder, which is replaced (URL-escaped) with the current user id. Defaults to
    /// <c>users/{userId}/allowed-requests</c>. The response body is <c>{ "requests": [ … ] }</c>.
    /// </summary>
    public string AllowedRequestsPath { get; set; } = "users/{userId}/allowed-requests";

    /// <summary>Per-call HTTP timeout (seconds). Defaults to 30.</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>How the outbound calls authenticate to the API. Defaults to <see cref="AuthorizationApiAuthenticationMode.None"/>.</summary>
    public AuthorizationApiAuthenticationMode AuthenticationMode { get; set; } = AuthorizationApiAuthenticationMode.None;

    /// <summary>Header used when <see cref="AuthenticationMode"/> is <see cref="AuthorizationApiAuthenticationMode.ApiKey"/>. Defaults to <c>X-Api-Key</c>.</summary>
    public string ApiKeyHeaderName { get; set; } = "X-Api-Key";

    /// <summary>API key value sent when <see cref="AuthenticationMode"/> is <see cref="AuthorizationApiAuthenticationMode.ApiKey"/>.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Bearer token sent when <see cref="AuthenticationMode"/> is <see cref="AuthorizationApiAuthenticationMode.Bearer"/>.</summary>
    public string? BearerToken { get; set; }

    /// <summary>
    /// When <see langword="true"/> the resolved request sets are cached in <c>IDistributedCache</c>. A
    /// distributed cache must be registered, otherwise registration validation fails. Defaults to
    /// <see langword="false"/>.
    /// <para>
    /// Cached allow (or deny) sets remain in effect until TTL expires — revocation and rule changes are
    /// delayed for the process/consumers that still hold a cache entry. Prefer short
    /// <see cref="UserCacheDuration"/> values, or disable caching when immediate revocation is required.
    /// </para>
    /// </summary>
    public bool EnableCache { get; set; }

    /// <summary>Prefix applied to every cache key. Defaults to <c>raccoonland:authz:</c>.</summary>
    public string CacheKeyPrefix { get; set; } = "raccoonland:authz:";

    /// <summary>How long the anonymous request set is cached. Defaults to 5 minutes.</summary>
    public TimeSpan AnonymousCacheDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>How long each user's allowed request set is cached. Defaults to 1 minute.</summary>
    public TimeSpan UserCacheDuration { get; set; } = TimeSpan.FromMinutes(1);
}
