using System.Text;
using System.Text.Json;
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
    public async Task InvokeAsync_RoundTripsFullSuccessfulPipelineResponse()
    {
        var cache = new FakeDistributedCache();
        var middleware = RequestCachingTestHelpers.CreateMiddleware(cache);
        var request = new CacheableQuery { CacheKey = "round-trip" };

        var original = new PipelineResponse
        {
            Result = new Dictionary<string, object?>
            {
                ["id"] = 42,
                ["name"] = "widget",
                ["note"] = null,
            },
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

        var hitContext = RequestCachingTestHelpers.CreateContext(new CacheableQuery { CacheKey = "round-trip" });
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

        using var resultDoc = JsonDocument.Parse(JsonSerializer.Serialize(hitContext.Response.Result));
        Assert.Equal(42, resultDoc.RootElement.GetProperty("id").GetInt32());
        Assert.Equal("widget", resultDoc.RootElement.GetProperty("name").GetString());
        Assert.Equal(JsonValueKind.Null, resultDoc.RootElement.GetProperty("note").ValueKind);
    }
}
