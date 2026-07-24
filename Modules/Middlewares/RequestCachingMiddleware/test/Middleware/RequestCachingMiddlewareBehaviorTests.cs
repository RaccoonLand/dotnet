using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;
using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;
using RaccoonLand.Modules.Middlewares.RequestCachingMiddleware.Tests.Support;

namespace RaccoonLand.Modules.Middlewares.RequestCachingMiddleware.Tests.Middleware;

public sealed class RequestCachingMiddlewareBehaviorTests
{
    [Fact]
    public async Task InvokeAsync_WhenRequestIsNotCacheable_CallsNextWithoutCacheAccess()
    {
        var cache = new FakeDistributedCache();
        var middleware = RequestCachingTestHelpers.CreateMiddleware(cache);
        var context = RequestCachingTestHelpers.CreateContext(new PlainQuery());
        var nextCalled = false;

        await middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
        Assert.Equal(0, cache.GetCount);
        Assert.Equal(0, cache.SetCount);
    }

    [Fact]
    public async Task InvokeAsync_WhenRequestHasNoTypedResponse_BypassesCacheAndCallsNext()
    {
        var cache = new FakeDistributedCache();
        var middleware = RequestCachingTestHelpers.CreateMiddleware(cache);
        var context = RequestCachingTestHelpers.CreateContext(new VoidCacheableCommand());
        var nextCalled = false;

        await middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
        Assert.Equal(0, cache.GetCount);
        Assert.Equal(0, cache.SetCount);
    }

    [Fact]
    public async Task InvokeAsync_CacheKey_IsFullNameColonGetCacheKey()
    {
        var cache = new FakeDistributedCache();
        var middleware = RequestCachingTestHelpers.CreateMiddleware(cache);
        var request = new CacheableQuery { CacheKey = "abc-123" };
        var context = RequestCachingTestHelpers.CreateContext(request);
        var expectedKey = $"{typeof(CacheableQuery).FullName}:abc-123";

        await middleware.InvokeAsync(context, ctx =>
        {
            ctx.Response = new PipelineResponse { Result = "ok" };
            return Task.CompletedTask;
        });

        Assert.Single(cache.Sets);
        Assert.Equal(expectedKey, cache.Sets[0].Key);
    }

    [Fact]
    public async Task InvokeAsync_OnCacheHit_SetsResponseAndSkipsNext()
    {
        var cache = new FakeDistributedCache();
        var request = new CacheableQuery { CacheKey = "hit" };
        var key = $"{typeof(CacheableQuery).FullName}:hit";
        var cached = new PipelineResponse { Result = "from-cache" };
        cache.Seed(key, JsonSerializer.SerializeToUtf8Bytes(cached));

        var middleware = RequestCachingTestHelpers.CreateMiddleware(cache);
        var context = RequestCachingTestHelpers.CreateContext(request);
        var nextCalled = false;

        await middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.False(nextCalled);
        Assert.NotNull(context.Response);
        Assert.Equal("\"from-cache\"", System.Text.Json.JsonSerializer.Serialize(context.Response.Result));
        Assert.Equal(0, cache.SetCount);
    }

    [Fact]
    public async Task InvokeAsync_OnCacheMiss_CallsNextAndCachesSuccessfulResponse()
    {
        var cache = new FakeDistributedCache();
        var middleware = RequestCachingTestHelpers.CreateMiddleware(cache);
        var request = new CacheableQuery { CacheKey = "miss" };
        var context = RequestCachingTestHelpers.CreateContext(request);
        var nextCalled = false;

        await middleware.InvokeAsync(context, ctx =>
        {
            nextCalled = true;
            ctx.Response = new PipelineResponse { Result = "fresh" };
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
        Assert.Equal(1, cache.SetCount);
        Assert.Equal($"{typeof(CacheableQuery).FullName}:miss", cache.Sets[0].Key);
    }

    [Fact]
    public async Task InvokeAsync_WhenResponseHasErrors_DoesNotCache()
    {
        var cache = new FakeDistributedCache();
        var middleware = RequestCachingTestHelpers.CreateMiddleware(cache);
        var context = RequestCachingTestHelpers.CreateContext(new CacheableQuery { CacheKey = "err" });

        await middleware.InvokeAsync(context, ctx =>
        {
            ctx.Response = new PipelineResponse
            {
                Errors = [new PipelineMessage("E", "failed")],
            };
            return Task.CompletedTask;
        });

        Assert.Equal(0, cache.SetCount);
    }

    [Fact]
    public async Task InvokeAsync_WhenResponseIsNull_DoesNotCache()
    {
        var cache = new FakeDistributedCache();
        var middleware = RequestCachingTestHelpers.CreateMiddleware(cache);
        var context = RequestCachingTestHelpers.CreateContext(new CacheableQuery { CacheKey = "null" });

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        Assert.Null(context.Response);
        Assert.Equal(0, cache.SetCount);
    }

    [Fact]
    public async Task InvokeAsync_RoundTripsFullSuccessfulPipelineResponse_PreservesTypedResult()
    {
        var cache = new FakeDistributedCache();
        var middleware = RequestCachingTestHelpers.CreateMiddleware(cache);
        var request = new CacheableWidgetQuery { CacheKey = "round-trip" };

        var original = new PipelineResponse
        {
            Result = new WidgetResponse(42, "widget", null),
            Errors = [],
            Warnings =
            [
                new PipelineMessage("W1", "first-warning"),
                new PipelineMessage("W2", "second-warning"),
            ],
            StatusHint = null,
        };

        var missContext = RequestCachingTestHelpers.CreateContext(request);
        await middleware.InvokeAsync(missContext, ctx =>
        {
            ctx.Response = original;
            return Task.CompletedTask;
        });

        Assert.Equal(1, cache.SetCount);

        var hitContext = RequestCachingTestHelpers.CreateContext(new CacheableWidgetQuery { CacheKey = "round-trip" });
        var nextCalled = false;
        await middleware.InvokeAsync(hitContext, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.False(nextCalled);
        Assert.NotNull(hitContext.Response);
        Assert.Empty(hitContext.Response.Errors);
        Assert.Null(hitContext.Response.StatusHint);
        Assert.Equal(2, hitContext.Response.Warnings.Count);
        Assert.Equal(new PipelineMessage("W1", "first-warning"), hitContext.Response.Warnings[0]);
        Assert.Equal(new PipelineMessage("W2", "second-warning"), hitContext.Response.Warnings[1]);

        // Regression: the cached Result MUST come back as the request's TResponse (WidgetResponse),
        // not an untyped JsonElement. Consuming code relies on this shape.
        var typedResult = Assert.IsType<WidgetResponse>(hitContext.Response.Result);
        Assert.Equal(new WidgetResponse(42, "widget", null), typedResult);
    }

    [Fact]
    public async Task InvokeAsync_OnCacheHit_RehydratesPrimitiveResultAsOriginalType()
    {
        var cache = new FakeDistributedCache();
        var middleware = RequestCachingTestHelpers.CreateMiddleware(cache);
        var request = new CacheableIntQuery { CacheKey = "int-result" };

        var missContext = RequestCachingTestHelpers.CreateContext(request);
        await middleware.InvokeAsync(missContext, ctx =>
        {
            ctx.Response = new PipelineResponse { Result = 42 };
            return Task.CompletedTask;
        });

        var hitContext = RequestCachingTestHelpers.CreateContext(new CacheableIntQuery { CacheKey = "int-result" });
        await middleware.InvokeAsync(hitContext, _ => Task.CompletedTask);

        Assert.NotNull(hitContext.Response);
        var typedResult = Assert.IsType<int>(hitContext.Response.Result);
        Assert.Equal(42, typedResult);
    }

    [Fact]
    public async Task InvokeAsync_UsesResponseTypeFromMetadata_NotFromInterfaceReflection()
    {
        // Guardrail: the middleware must read TResponse from PipelineContext.Metadata (populated once at
        // startup by the dispatcher) and must NOT re-inspect the request's interfaces at request time.
        // We prove this by hand-building a context whose Metadata claims a WRONG response type; the
        // middleware should honor the metadata and (on a miss) still cache and (on a hit) rehydrate under
        // that declared type. A real production context always carries metadata that matches the request.
        var cache = new FakeDistributedCache();
        var middleware = RequestCachingTestHelpers.CreateMiddleware(cache);
        var request = new CacheableIntQuery { CacheKey = "metadata-driven" };

        // Bypass the helper (which uses RequestMetadata.For, i.e. the correct interface walk) and build
        // Metadata by hand with a lying ResponseType so a difference from IRequest<int> is observable.
        var metadata = new RequestMetadata(typeof(CacheableIntQuery), typeof(long), RequestKind.Query);
        var missContext = new PipelineContext(
            request,
            new ServiceCollection().BuildServiceProvider(),
            metadata);

        await middleware.InvokeAsync(missContext, ctx =>
        {
            ctx.Response = new PipelineResponse { Result = 7L };
            return Task.CompletedTask;
        });

        Assert.Equal(1, cache.SetCount);

        var hitContext = new PipelineContext(
            new CacheableIntQuery { CacheKey = "metadata-driven" },
            new ServiceCollection().BuildServiceProvider(),
            metadata);
        var nextCalled = false;
        await middleware.InvokeAsync(hitContext, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.False(nextCalled);
        Assert.NotNull(hitContext.Response);
        // Rehydrated under the metadata's claimed type, not IRequest<int>.
        Assert.IsType<long>(hitContext.Response.Result);
        Assert.Equal(7L, (long)hitContext.Response.Result!);
    }

    [Fact]
    public async Task InvokeAsync_OnCacheHit_WhenResultIsNull_LeavesResultNull()
    {
        var cache = new FakeDistributedCache();
        var middleware = RequestCachingTestHelpers.CreateMiddleware(cache);
        var request = new CacheableWidgetQuery { CacheKey = "null-result" };

        var missContext = RequestCachingTestHelpers.CreateContext(request);
        await middleware.InvokeAsync(missContext, ctx =>
        {
            ctx.Response = new PipelineResponse
            {
                Result = null,
                Warnings = [new PipelineMessage("W", "n/a")],
            };
            return Task.CompletedTask;
        });

        var hitContext = RequestCachingTestHelpers.CreateContext(new CacheableWidgetQuery { CacheKey = "null-result" });
        var nextCalled = false;
        await middleware.InvokeAsync(hitContext, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.False(nextCalled);
        Assert.NotNull(hitContext.Response);
        Assert.Null(hitContext.Response.Result);
        Assert.Single(hitContext.Response.Warnings);
    }
}
