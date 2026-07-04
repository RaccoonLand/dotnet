using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;
using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;
using RaccoonLand.Modules.Middlewares.RequestCaching.Abstraction;

namespace RaccoonLand.Modules.Middlewares.RequestCachingMiddleware;

/// <summary>
/// Read-through caching for requests that implement <see cref="ICacheableRequest"/>. On a cache hit the cached
/// response is returned and the rest of the pipeline (and the endpoint) is skipped; on a miss the pipeline runs
/// and the produced response is written back to the cache. The response is written only on a miss — reads never
/// refresh the entry.
/// <para>
/// Storage uses <see cref="IDistributedCache"/>, so the in-memory implementation
/// (<c>AddDistributedMemoryCache</c>) or any out-of-process provider (Redis, SQL Server, ...) works unchanged.
/// All cache access is guarded: any failure is logged and the request continues as if caching were absent.
/// </para>
/// </summary>
public sealed class RequestCachingMiddleware(
    IDistributedCache cache,
    IOptions<RequestCachingOptions> options,
    ILogger<RequestCachingMiddleware> logger) : IPipelineMiddleware
{
    private readonly IDistributedCache _cache = cache;
    private readonly RequestCachingOptions _options = options.Value;
    private readonly ILogger<RequestCachingMiddleware> _logger = logger;

    public async Task InvokeAsync(PipelineContext context, PipelineDelegate next)
    {
        if (context.Request is not ICacheableRequest cacheable)
        {
            await next(context);
            return;
        }

        var requestType = context.Request.GetType();

        // No typed response means there is nothing meaningful to cache (e.g. a void command).
        if (!HasTypedResponse(requestType))
        {
            await next(context);
            return;
        }

        var key = BuildKey(requestType, cacheable.GetCacheKey());

        var hit = await TryGetFromCacheAsync(key, context.CancellationToken);
        if (hit.Found)
        {
            context.Response = hit.Value;
            return;
        }

        await next(context);

        if (context.Response is not null)
        {
            await SetCacheAsync(key, requestType, context.Response, context.CancellationToken);
        }
    }

    private async Task<CacheReadResult> TryGetFromCacheAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            var bytes = await _cache.GetAsync(key, cancellationToken);
            if (bytes is null || bytes.Length == 0)
            {
                return CacheReadResult.Miss;
            }

            var value = JsonSerializer.Deserialize<PipelineResponse>(bytes);
            return new CacheReadResult(true, value);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Reading request cache failed for key {CacheKey}; continuing without cache.", key);
            return CacheReadResult.Miss;
        }
    }

    private async Task SetCacheAsync(
        string key,
        Type requestType,
        PipelineResponse response,
        CancellationToken cancellationToken)
    {
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(response);
            var entryOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ResolveDuration(requestType),
            };

            await _cache.SetAsync(key, bytes, entryOptions, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Writing request cache failed for key {CacheKey}; continuing.", key);
        }
    }

    /// <summary>
    /// Resolves the lifetime for a request category using the longest matching override prefix, mirroring how
    /// logging resolves a log level from a category name.
    /// </summary>
    private TimeSpan ResolveDuration(Type requestType)
    {
        var options = _options;
        var category = requestType.FullName ?? requestType.Name;

        var best = options.Default;
        var bestLength = -1;

        foreach (var (prefix, entry) in options.Overrides)
        {
            var isMatch = string.Equals(category, prefix, StringComparison.OrdinalIgnoreCase)
                || category.StartsWith(prefix + ".", StringComparison.OrdinalIgnoreCase);

            if (isMatch && prefix.Length > bestLength)
            {
                best = entry;
                bestLength = prefix.Length;
            }
        }

        return best.Duration;
    }

    private static string BuildKey(Type requestType, string cacheKey)
        => $"{requestType.FullName}:{cacheKey}";

    private readonly record struct CacheReadResult(bool Found, PipelineResponse? Value)
    {
        public static CacheReadResult Miss => new(false, null);
    }

    private static bool HasTypedResponse(Type requestType)
    {
        foreach (var @interface in requestType.GetInterfaces())
        {
            if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IRequest<>))
            {
                return true;
            }
        }

        return false;
    }
}
