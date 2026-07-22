using System.Text;
using System.Text.Json;
using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;
using RaccoonLand.Modules.Middlewares.RequestCachingMiddleware.Tests.Support;

namespace RaccoonLand.Modules.Middlewares.RequestCachingMiddleware.Tests.Middleware;

public sealed class RequestCachingMiddlewareFailureTests
{
    [Fact]
    public async Task InvokeAsync_WhenCacheReadFails_ContinuesAsMiss()
    {
        var cache = new FakeDistributedCache
        {
            GetException = new InvalidOperationException("read-failed"),
        };
        var middleware = RequestCachingTestHelpers.CreateMiddleware(cache);
        var context = RequestCachingTestHelpers.CreateContext(new CacheableQuery { CacheKey = "read" });
        var nextCalled = false;

        await middleware.InvokeAsync(context, ctx =>
        {
            nextCalled = true;
            ctx.Response = new PipelineResponse { Result = "ok" };
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
        Assert.Equal("ok", context.Response?.Result);
        Assert.Equal(1, cache.SetCount);
    }

    [Fact]
    public async Task InvokeAsync_WhenDeserializeYieldsNull_RemovesEntryAndContinuesAsMiss()
    {
        var cache = new FakeDistributedCache();
        var request = new CacheableQuery { CacheKey = "corrupt" };
        var key = $"{typeof(CacheableQuery).FullName}:corrupt";
        cache.Seed(key, Encoding.UTF8.GetBytes("null"));

        var middleware = RequestCachingTestHelpers.CreateMiddleware(cache);
        var context = RequestCachingTestHelpers.CreateContext(request);
        var nextCalled = false;

        await middleware.InvokeAsync(context, ctx =>
        {
            nextCalled = true;
            ctx.Response = new PipelineResponse { Result = "rebuilt" };
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
        Assert.Contains(key, cache.RemovedKeys);
        Assert.Equal("rebuilt", context.Response?.Result);
        Assert.Equal(1, cache.SetCount);
    }

    [Fact]
    public async Task InvokeAsync_WhenRemoveCorruptEntryFails_ContinuesAsMiss()
    {
        var cache = new FakeDistributedCache
        {
            RemoveException = new InvalidOperationException("remove-failed"),
        };
        var request = new CacheableQuery { CacheKey = "corrupt-remove" };
        var key = $"{typeof(CacheableQuery).FullName}:corrupt-remove";
        cache.Seed(key, Encoding.UTF8.GetBytes("null"));

        var middleware = RequestCachingTestHelpers.CreateMiddleware(cache);
        var context = RequestCachingTestHelpers.CreateContext(request);
        var nextCalled = false;

        await middleware.InvokeAsync(context, ctx =>
        {
            nextCalled = true;
            ctx.Response = new PipelineResponse { Result = "ok" };
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
        Assert.Equal(1, cache.RemoveCount);
        Assert.Equal("ok", context.Response?.Result);
    }

    [Fact]
    public async Task InvokeAsync_WhenCacheWriteFails_DoesNotFailRequest()
    {
        var cache = new FakeDistributedCache
        {
            SetException = new InvalidOperationException("write-failed"),
        };
        var middleware = RequestCachingTestHelpers.CreateMiddleware(cache);
        var context = RequestCachingTestHelpers.CreateContext(new CacheableQuery { CacheKey = "write" });

        await middleware.InvokeAsync(context, ctx =>
        {
            ctx.Response = new PipelineResponse { Result = "ok" };
            return Task.CompletedTask;
        });

        Assert.Equal("ok", context.Response?.Result);
        Assert.Equal(1, cache.SetCount);
    }

    [Fact]
    public async Task InvokeAsync_WhenTokenCanceledOnRead_RethrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var cache = new FakeDistributedCache
        {
            GetException = new OperationCanceledException(cts.Token),
        };
        var middleware = RequestCachingTestHelpers.CreateMiddleware(cache);
        var context = RequestCachingTestHelpers.CreateContext(
            new CacheableQuery { CacheKey = "cancel-read" },
            cts.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => middleware.InvokeAsync(context, _ => Task.CompletedTask));
    }

    [Fact]
    public async Task InvokeAsync_WhenTokenCanceledOnWrite_RethrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        var cache = new FakeDistributedCache();
        var middleware = RequestCachingTestHelpers.CreateMiddleware(cache);
        var context = RequestCachingTestHelpers.CreateContext(
            new CacheableQuery { CacheKey = "cancel-write" },
            cts.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => middleware.InvokeAsync(context, async ctx =>
        {
            ctx.Response = new PipelineResponse { Result = "ok" };
            await cts.CancelAsync();
            cache.SetException = new OperationCanceledException(cts.Token);
        }));
    }
}
