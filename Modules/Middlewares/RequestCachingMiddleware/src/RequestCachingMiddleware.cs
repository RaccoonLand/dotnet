using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;
using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;
using RaccoonLand.Modules.Middlewares.RequestCaching.Abstraction;

namespace RaccoonLand.Modules.Middlewares.RequestCachingMiddleware;

/// <summary>
/// Read-through caching for requests that implement <see cref="ICacheableRequest"/>. On a cache hit the cached
/// response is returned and the rest of the pipeline (and the endpoint) is skipped; on a miss the pipeline runs
/// and a successful response (no <see cref="PipelineResponse.Errors"/>) is written back to the cache. The
/// response is written only on a miss — reads never refresh the entry.
/// <para>
/// Storage uses <see cref="IDistributedCache"/>, so the in-memory implementation
/// (<c>AddDistributedMemoryCache</c>) or any out-of-process provider (Redis, SQL Server, ...) works unchanged.
/// Cache infrastructure failures (including best-effort removal of a corrupt null entry) are logged and the
/// request continues as if caching were absent (fail-open).
/// <see cref="OperationCanceledException"/> for the request token is rethrown.
/// </para>
/// </summary>
public sealed class RequestCachingMiddleware(
    IDistributedCache cache,
    IOptionsMonitor<RequestCachingOptions> options,
    ILogger<RequestCachingMiddleware> logger) : IPipelineMiddleware
{
    private readonly IDistributedCache _cache = cache;
    private readonly IOptionsMonitor<RequestCachingOptions> _options = options;
    private readonly ILogger<RequestCachingMiddleware> _logger = logger;

    public async Task InvokeAsync(PipelineContext context, PipelineDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (context.Request is not ICacheableRequest cacheable)
        {
            await next(context);
            return;
        }

        var metadata = context.Metadata;

        // No typed response means there is nothing meaningful to cache (e.g. a void command).
        // Metadata is captured at startup by the endpoint scan, so this is a single field read — no
        // per-request reflection walk over the request's interfaces.
        if (metadata.ResponseType is not { } responseType)
        {
            await next(context);
            return;
        }

        var requestType = metadata.RequestType;
        var key = BuildKey(requestType, cacheable.GetCacheKey());

        var hit = await TryGetFromCacheAsync(key, responseType, context.CancellationToken);
        if (hit.Found)
        {
            context.Response = hit.Value;
            return;
        }

        await next(context);

        if (context.Response is not null && IsCacheableResponse(context.Response))
        {
            await SetCacheAsync(key, requestType, context.Response, context.CancellationToken);
        }
    }

    private async Task<CacheReadResult> TryGetFromCacheAsync(
        string key,
        Type responseType,
        CancellationToken cancellationToken)
    {
        try
        {
            var bytes = await _cache.GetAsync(key, cancellationToken);
            if (bytes is null || bytes.Length == 0)
            {
                return CacheReadResult.Miss;
            }

            CachedEnvelope? envelope;
            try
            {
                envelope = JsonSerializer.Deserialize<CachedEnvelope>(bytes);
            }
            catch (JsonException jsonException)
            {
                _logger.LogWarning(
                    jsonException,
                    "Cached entry for key {CacheKey} is not valid JSON; treating as a cache miss and removing the entry.",
                    key);
                await TryRemoveCorruptEntryAsync(key, cancellationToken);
                return CacheReadResult.Miss;
            }

            if (envelope is null)
            {
                _logger.LogWarning(
                    "Cached entry for key {CacheKey} deserialized to null; treating as a cache miss and removing the entry.",
                    key);
                await TryRemoveCorruptEntryAsync(key, cancellationToken);
                return CacheReadResult.Miss;
            }

            object? typedResult = null;
            if (envelope.Result is JsonElement element && element.ValueKind != JsonValueKind.Null)
            {
                try
                {
                    typedResult = element.Deserialize(responseType);
                }
                catch (JsonException jsonException)
                {
                    _logger.LogWarning(
                        jsonException,
                        "Cached entry for key {CacheKey} has a Result that cannot be rehydrated to {ResponseType}; treating as a cache miss and removing the entry.",
                        key,
                        responseType);
                    await TryRemoveCorruptEntryAsync(key, cancellationToken);
                    return CacheReadResult.Miss;
                }
            }

            var response = new PipelineResponse
            {
                Result = typedResult,
                Errors = envelope.Errors ?? [],
                Warnings = envelope.Warnings ?? [],
                StatusHint = envelope.StatusHint,
            };

            return new CacheReadResult(true, response);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Reading request cache failed for key {CacheKey}; continuing without cache.", key);
            return CacheReadResult.Miss;
        }
    }

    /// <summary>
    /// Best-effort removal of a corrupt cache entry so subsequent requests do not keep re-reading it.
    /// Removal failures are fail-open; cancellation is rethrown.
    /// </summary>
    private async Task TryRemoveCorruptEntryAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Removing corrupt cache entry failed for key {CacheKey}; continuing.",
                key);
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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Writing request cache failed for key {CacheKey}; continuing.", key);
        }
    }

    /// <summary>
    /// Resolves the lifetime for a request category using the longest matching override prefix, mirroring how
    /// logging resolves a log level from a category name. Options are read from
    /// <see cref="IOptionsMonitor{TOptions}.CurrentValue"/> so configuration reloads apply.
    /// </summary>
    private TimeSpan ResolveDuration(Type requestType)
    {
        var options = _options.CurrentValue;
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

    /// <summary>
    /// Successful envelopes only — responses with errors are not cached so transient failures are not replayed.
    /// </summary>
    private static bool IsCacheableResponse(PipelineResponse response)
        => response.Errors.Count == 0;

    private readonly record struct CacheReadResult(bool Found, PipelineResponse? Value)
    {
        public static CacheReadResult Miss => new(false, null);
    }

    /// <summary>
    /// Wire format for cached envelopes. We store <see cref="PipelineResponse.Result"/> as a raw
    /// <see cref="JsonElement"/> so it can be re-hydrated back into the caller's <c>TResponse</c> on read;
    /// deserializing straight into <see cref="PipelineResponse"/> would leave <c>Result</c> as an untyped
    /// <see cref="JsonElement"/> and silently break typed consumers.
    /// </summary>
    private sealed record CachedEnvelope
    {
        public JsonElement? Result { get; init; }

        public IReadOnlyList<PipelineMessage>? Errors { get; init; }

        public IReadOnlyList<PipelineMessage>? Warnings { get; init; }

        public int? StatusHint { get; init; }
    }
}
