using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Modules.Middlewares.RequestCaching.Abstraction;

namespace RaccoonLand.Modules.Middlewares.RequestCachingMiddleware.Tests.Support;

internal sealed class PlainQuery : IRequest<string>;

internal sealed class VoidCacheableCommand : IRequest, ICacheableRequest
{
    public string GetCacheKey() => "void-key";
}

internal sealed class CacheableQuery : IRequest<string>, ICacheableRequest
{
    public required string CacheKey { get; init; }

    public string GetCacheKey() => CacheKey;
}
