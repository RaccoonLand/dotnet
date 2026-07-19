using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using RaccoonLand.Core.ExecutionContext.Abstractions;
using RaccoonLand.Modules.Security.Authorization.Abstractions;
using RaccoonLand.Modules.Security.Authorization.SqlServer.Configuration;
using RaccoonLand.Modules.Security.Authorization.SqlServer.Data;

namespace RaccoonLand.Modules.Security.Authorization.SqlServer.Provider;

/// <summary>
/// An <see cref="IAuthorizationProvider"/> backed by two stored procedures. It returns only a status — codes
/// and messages are the middleware's responsibility. Deny-by-default:
/// <list type="number">
///   <item><description>request is in the anonymous set → <see cref="AuthorizationStatus.Allowed"/>;</description></item>
///   <item><description>no current user id → <see cref="AuthorizationStatus.Unauthenticated"/>;</description></item>
///   <item><description>request is in the user's allowed set → <see cref="AuthorizationStatus.Allowed"/>;</description></item>
///   <item><description>otherwise → <see cref="AuthorizationStatus.Denied"/>.</description></item>
/// </list>
/// When <see cref="SqlAuthorizationOptions.EnableCache"/> is set, the anonymous set and each user's allowed
/// set are cached in <see cref="IDistributedCache"/>. Cache entries can keep a previous allow (or deny)
/// decision until TTL expires after revocation or rule changes.
/// Database and connectivity failures propagate as exceptions and are not mapped to
/// <see cref="AuthorizationStatus.Denied"/>.
/// </summary>
public sealed class SqlAuthorizationProvider : IAuthorizationProvider
{
    private readonly SqlAuthorizationRepository _repository;
    private readonly ICurrentExecutionContext _executionContext;
    private readonly IDistributedCache? _cache;
    private readonly SqlAuthorizationOptions _options;

    public SqlAuthorizationProvider(
        SqlAuthorizationRepository repository,
        ICurrentExecutionContext executionContext,
        IOptions<SqlAuthorizationOptions> options,
        IDistributedCache? cache = null)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(executionContext);
        ArgumentNullException.ThrowIfNull(options);

        _repository = repository;
        _executionContext = executionContext;
        _options = options.Value;
        _cache = cache;

        if (_options.EnableCache && _cache is null)
        {
            throw new InvalidOperationException(
                "SqlAuthorizationOptions.EnableCache is true but no IDistributedCache is registered. " +
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
            static (repository, ct) => repository.GetAnonymousRequestsAsync(ct),
            cancellationToken);

    private Task<IReadOnlyCollection<string>> GetAllowedRequestsAsync(string userId, CancellationToken cancellationToken)
        => GetOrLoadAsync(
            _options.CacheKeyPrefix + "user:" + userId,
            _options.UserCacheDuration,
            (repository, ct) => repository.GetAllowedRequestsAsync(userId, ct),
            cancellationToken);

    private async Task<IReadOnlyCollection<string>> GetOrLoadAsync(
        string cacheKey,
        TimeSpan duration,
        Func<SqlAuthorizationRepository, CancellationToken, Task<IReadOnlyCollection<string>>> load,
        CancellationToken cancellationToken)
    {
        if (!_options.EnableCache)
        {
            return await load(_repository, cancellationToken);
        }

        var cached = await _cache!.GetStringAsync(cacheKey, cancellationToken);
        if (cached is not null)
        {
            var names = JsonSerializer.Deserialize<string[]>(cached) ?? [];
            return new HashSet<string>(names, StringComparer.Ordinal);
        }

        var loaded = await load(_repository, cancellationToken);

        await _cache!.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(loaded),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = duration },
            cancellationToken);

        return loaded;
    }
}
