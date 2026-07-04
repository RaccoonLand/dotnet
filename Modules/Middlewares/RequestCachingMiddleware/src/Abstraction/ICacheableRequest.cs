namespace RaccoonLand.Modules.Middlewares.RequestCaching.Abstraction;

/// <summary>
/// Marks a request whose response can be cached by
/// <c>RaccoonLand.Modules.Middlewares.RequestCachingMiddleware.RequestCachingMiddleware</c>.
/// Implement this on a command or query (typically a query) to opt the request into read-through caching.
/// </summary>
public interface ICacheableRequest
{
    /// <summary>
    /// Returns the stable cache key for this request instance (for example built from its identifying
    /// values). The middleware namespaces this key by the request type, so the key only needs to be
    /// unique across instances of the same request type.
    /// </summary>
    string GetCacheKey();
}
