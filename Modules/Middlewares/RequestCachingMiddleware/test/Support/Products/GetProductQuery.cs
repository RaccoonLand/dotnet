using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Modules.Middlewares.RequestCaching.Abstraction;

namespace RaccoonLand.Modules.Middlewares.RequestCachingMiddleware.Tests.Support.Products;

internal sealed class GetProductQuery : IRequest<string>, ICacheableRequest
{
    public string GetCacheKey() => "product-1";
}
