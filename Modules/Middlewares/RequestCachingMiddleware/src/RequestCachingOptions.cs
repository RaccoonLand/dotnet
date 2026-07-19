namespace RaccoonLand.Modules.Middlewares.RequestCachingMiddleware;

/// <summary>
/// Options for <see cref="RequestCachingMiddleware"/>. A single <see cref="Default"/> entry applies to every
/// cacheable request, while <see cref="Overrides"/> lets you tune the lifetime per request category, exactly
/// like log levels: the key is matched against the request type's full name (namespace + type name) and the
/// most specific (longest) matching prefix wins.
/// </summary>
/// <example>
/// appsettings.json:
/// <code>
/// "RequestCaching": {
///   "Default": { "Duration": "00:05:00" },
///   "Overrides": {
///     "MyApp.Features.Products": { "Duration": "00:00:30" },
///     "MyApp.Features.Products.GetProductQuery": { "Duration": "00:02:00" }
///   }
/// }
/// </code>
/// </example>
public sealed class RequestCachingOptions
{
    /// <summary>Default root configuration section name (<c>RequestCaching</c>).</summary>
    public const string SectionName = "RequestCaching";

    /// <summary>The fallback entry applied when no override matches the request category.</summary>
    public RequestCacheEntryOptions Default { get; set; } = new();

    /// <summary>
    /// Per-category overrides keyed by a request-type-name prefix (namespace + type name). Matching follows
    /// the logging convention: the longest key that is a prefix of the request's full type name wins.
    /// </summary>
    public IDictionary<string, RequestCacheEntryOptions> Overrides { get; set; }
        = new Dictionary<string, RequestCacheEntryOptions>(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Lifetime settings for a cached request response.</summary>
public sealed class RequestCacheEntryOptions
{
    /// <summary>
    /// Absolute time-to-live for the cached entry. Must be greater than <see cref="TimeSpan.Zero"/>.
    /// Absolute (not sliding) expiration is used on purpose so that reads never rewrite the entry — the cache
    /// is only written on a miss.
    /// </summary>
    public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(5);
}
