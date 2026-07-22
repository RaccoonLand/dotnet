using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;
using RaccoonLand.Modules.Middlewares.RequestCachingMiddleware.Tests.Support;

namespace RaccoonLand.Modules.Middlewares.RequestCachingMiddleware.Tests.Support;

internal static class RequestCachingTestHelpers
{
    public static RequestCachingMiddleware CreateMiddleware(
        FakeDistributedCache cache,
        RequestCachingOptions? options = null)
    {
        var monitor = new TestOptionsMonitor<RequestCachingOptions>(options ?? new RequestCachingOptions());
        return new RequestCachingMiddleware(cache, monitor, NullLogger<RequestCachingMiddleware>.Instance);
    }

    public static PipelineContext CreateContext(
        IRequestBase request,
        CancellationToken cancellationToken = default)
    {
        return new PipelineContext(
            request,
            RequestKind.Query,
            new ServiceCollection().BuildServiceProvider(),
            cancellationToken);
    }
}
