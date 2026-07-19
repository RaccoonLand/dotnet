using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using RaccoonLand.Core.ExecutionContext.Abstractions;
using RaccoonLand.Modules.Security.Authorization.Abstractions;
using RaccoonLand.Modules.Security.Authorization.Api.Configuration;
using RaccoonLand.Modules.Security.Authorization.Api.Http;

namespace RaccoonLand.Modules.Security.Authorization.Api.Provider;

/// <summary>
/// An <see cref="IAuthorizationProvider"/> backed by an external authorization API
/// (see <see cref="AuthorizationApiClient"/>). It returns only a status — codes and messages are the
/// middleware's responsibility. Deny-by-default:
/// <list type="number">
///   <item><description>request is in the anonymous set → <see cref="AuthorizationStatus.Allowed"/>;</description></item>
///   <item><description>no current user id → <see cref="AuthorizationStatus.Unauthenticated"/>;</description></item>
///   <item><description>request is in the user's allowed set → <see cref="AuthorizationStatus.Allowed"/>;</description></item>
///   <item><description>otherwise → <see cref="AuthorizationStatus.Denied"/>.</description></item>
/// </list>
/// When <see cref="ApiAuthorizationOptions.EnableCache"/> is set, the anonymous set and each user's allowed set
/// are cached in <see cref="IDistributedCache"/>. Cache entries can keep a previous allow (or deny) decision
/// until TTL expires after revocation or rule changes.
/// Transport, timeout, and deserialization failures from the API propagate as exceptions and are not mapped
/// to <see cref="AuthorizationStatus.Denied"/>.
/// </summary>
public sealed class ApiAuthorizationProvider : IAuthorizationProvider
{
    private readonly AuthorizationApiClient _client;
    private readonly ICurrentExecutionContext _executionContext;
    private readonly IDistributedCache? _cache;
    private readonly ApiAuthorizationOptions _options;

    public ApiAuthorizationProvider(
        AuthorizationApiClient client,
        ICurrentExecutionContext executionContext,
        IOptions<ApiAuthorizationOptions> options,
        IDistributedCache? cache = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(executionContext);
        ArgumentNullException.ThrowIfNull(options);

        _client = client;
        _executionContext = executionContext;
        _options = options.Value;
        _cache = cache;

        if (_options.EnableCache && _cache is null)
        {
            throw new InvalidOperationException(
                "ApiAuthorizationOptions.EnableCache is true but no IDistributedCache is registered. " +
                "Register a distributed cache (for example services.AddDistributedMemoryCache()) or disable caching.");
        }
    }

    public async Task<AuthorizationDecision> AuthorizeAsync(
        AuthorizationContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var anonymous = await GetAnonymousRequestsAsync(cancellationToken);
        if (anonymous.Contains(context.RequestName))
        {
            return AuthorizationDecision.Allow();
        }

        var userId = _executionContext.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return AuthorizationDecision.Unauthenticated();
        }

        var allowed = await GetAllowedRequestsAsync(userId, cancellationToken);
        return allowed.Contains(context.RequestName)
            ? AuthorizationDecision.Allow()
            : AuthorizationDecision.Deny();
    }

    private Task<IReadOnlyCollection<string>> GetAnonymousRequestsAsync(CancellationToken cancellationToken)
        => GetOrLoadAsync(
            _options.CacheKeyPrefix + "anon",
            _options.AnonymousCacheDuration,
            static (client, ct) => client.GetAnonymousRequestsAsync(ct),
            cancellationToken);

    private Task<IReadOnlyCollection<string>> GetAllowedRequestsAsync(string userId, CancellationToken cancellationToken)
        => GetOrLoadAsync(
            _options.CacheKeyPrefix + "user:" + userId,
            _options.UserCacheDuration,
            (client, ct) => client.GetAllowedRequestsAsync(userId, ct),
            cancellationToken);

    private async Task<IReadOnlyCollection<string>> GetOrLoadAsync(
        string cacheKey,
        TimeSpan duration,
        Func<AuthorizationApiClient, CancellationToken, Task<IReadOnlyCollection<string>>> load,
        CancellationToken cancellationToken)
    {
        if (!_options.EnableCache)
        {
            return await load(_client, cancellationToken);
        }

        var cached = await _cache!.GetStringAsync(cacheKey, cancellationToken);
        if (cached is not null)
        {
            var names = JsonSerializer.Deserialize<string[]>(cached) ?? [];
            return new HashSet<string>(names, StringComparer.Ordinal);
        }

        var loaded = await load(_client, cancellationToken);

        await _cache!.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(loaded),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = duration },
            cancellationToken);

        return loaded;
    }
}
