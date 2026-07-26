namespace RaccoonLand.Modules.Security.Authorization.SqlServer.Configuration;

/// <summary>
/// Settings for the SQL Server authorization provider, typically bound from a configuration section such as
/// <c>appsettings.json</c>. The provider asks two stored procedures (named here, never hard-coded) for the
/// set of anonymous request names and the set of request names the current user may execute.
/// </summary>
/// <example>
/// <code>
/// "Authorization": {
///   "SqlServer": {
///     "ConnectionString": "Server=.;Database=Security;Trusted_Connection=True;TrustServerCertificate=True",
///     "ServiceName": "Ordering",
///     "ApplicationName": "Ordering.Api",
///     "AnonymousRequestsProcedure": "dbo.GetAnonymousRequests",
///     "AllowedRequestsProcedure": "dbo.GetAllowedRequestsForUser",
///     "UserIdParameterName": "UserId",
///     "EnableCache": true,
///     "AnonymousCacheDuration": "00:05:00",
///     "UserCacheDuration": "00:01:00"
///   }
/// }
/// </code>
/// </example>
public sealed class SqlAuthorizationOptions
{
    /// <summary>Default configuration section name (<c>Authorization:SqlServer</c>).</summary>
    public const string SectionName = "Authorization:SqlServer";

    /// <summary>Connection string to the database that hosts the authorization stored procedures. Required.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Optional name of the current microservice. When non-empty it is passed to both stored procedures
    /// (see <see cref="ServiceNameParameterName"/>) and included in cache keys so a shared authorization
    /// database / distributed cache stays isolated across services. Leave empty (default) for unscoped,
    /// single-service deployments.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Optional name of the current application within the service. Same semantics as
    /// <see cref="ServiceName"/> (see <see cref="ApplicationNameParameterName"/>). Prefer setting both
    /// together when sharing an authorization store across a microservice fleet.
    /// </summary>
    public string ApplicationName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the stored procedure that returns the publicly accessible (anonymous) request names.
    /// Returns a single column of request full-names. When <see cref="ServiceName"/> /
    /// <see cref="ApplicationName"/> are set, those values are passed as parameters. Required.
    /// </summary>
    public string AnonymousRequestsProcedure { get; set; } = string.Empty;

    /// <summary>
    /// Name of the stored procedure that returns the request names the current user may execute. It receives
    /// the user id (see <see cref="UserIdParameterName"/>) and, when configured, the optional service /
    /// application scope parameters. Returns a single column of request full-names. Required.
    /// </summary>
    public string AllowedRequestsProcedure { get; set; } = string.Empty;

    /// <summary>
    /// Name of the user-id parameter passed to <see cref="AllowedRequestsProcedure"/> (without the leading
    /// <c>@</c>). Defaults to <c>UserId</c>.
    /// </summary>
    public string UserIdParameterName { get; set; } = "UserId";

    /// <summary>
    /// Name of the service-name parameter passed to both procedures when <see cref="ServiceName"/> is set
    /// (without the leading <c>@</c>). Defaults to <c>ServiceName</c>.
    /// </summary>
    public string ServiceNameParameterName { get; set; } = "ServiceName";

    /// <summary>
    /// Name of the application-name parameter passed to both procedures when <see cref="ApplicationName"/> is
    /// set (without the leading <c>@</c>). Defaults to <c>ApplicationName</c>.
    /// </summary>
    public string ApplicationNameParameterName { get; set; } = "ApplicationName";

    /// <summary>Command timeout (seconds) applied to each stored-procedure call. Defaults to 30.</summary>
    public int CommandTimeoutSeconds { get; set; } = 30;

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
