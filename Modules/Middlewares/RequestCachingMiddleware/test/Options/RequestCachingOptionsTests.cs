using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;
using RaccoonLand.Modules.Middlewares.RequestCachingMiddleware.Tests.Support;
using RaccoonLand.Modules.Middlewares.RequestCachingMiddleware.Tests.Support.Products;

namespace RaccoonLand.Modules.Middlewares.RequestCachingMiddleware.Tests.Options;

public sealed class RequestCachingOptionsTests
{
    [Fact]
    public void DefaultDuration_IsFiveMinutes()
    {
        Assert.Equal(TimeSpan.FromMinutes(5), new RequestCacheEntryOptions().Duration);
        Assert.Equal(TimeSpan.FromMinutes(5), new RequestCachingOptions().Default.Duration);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ValidateDurations_RejectsNonPositiveDefault(int seconds)
    {
        var services = new ServiceCollection();
        services.AddRaccoonLandRequestCaching(o =>
            o.Default.Duration = TimeSpan.FromSeconds(seconds));

        var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<RequestCachingOptions>>().Value);
    }

    [Fact]
    public void ValidateDurations_AcceptsPositiveDefault()
    {
        var services = new ServiceCollection();
        services.AddRaccoonLandRequestCaching(o =>
            o.Default.Duration = TimeSpan.FromSeconds(30));

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<RequestCachingOptions>>().Value;

        Assert.Equal(TimeSpan.FromSeconds(30), options.Default.Duration);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ValidateDurations_RejectsNonPositiveOverride(int seconds)
    {
        var services = new ServiceCollection();
        services.AddRaccoonLandRequestCaching(o =>
        {
            o.Default.Duration = TimeSpan.FromMinutes(5);
            o.Overrides["Some.Prefix"] = new RequestCacheEntryOptions
            {
                Duration = TimeSpan.FromSeconds(seconds),
            };
        });

        var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<RequestCachingOptions>>().Value);
    }

    [Fact]
    public async Task ResolveDuration_LongestPrefixWinsOverShorterMatch()
    {
        var cache = new FakeDistributedCache();
        var productType = typeof(GetProductQuery);
        var namespacePrefix = productType.Namespace!;
        var exactPrefix = productType.FullName!;

        var options = new RequestCachingOptions
        {
            Default = new RequestCacheEntryOptions { Duration = TimeSpan.FromMinutes(5) },
            Overrides =
            {
                [namespacePrefix] = new RequestCacheEntryOptions { Duration = TimeSpan.FromSeconds(30) },
                [exactPrefix] = new RequestCacheEntryOptions { Duration = TimeSpan.FromMinutes(2) },
            },
        };

        var middleware = RequestCachingTestHelpers.CreateMiddleware(cache, options);
        var context = RequestCachingTestHelpers.CreateContext(new GetProductQuery());

        await middleware.InvokeAsync(context, ctx =>
        {
            ctx.Response = new PipelineResponse { Result = "p" };
            return Task.CompletedTask;
        });

        Assert.Equal(TimeSpan.FromMinutes(2), cache.Sets[0].Options.AbsoluteExpirationRelativeToNow);
    }

    [Fact]
    public async Task ResolveDuration_FallsBackToDefaultWhenNoOverrideMatches()
    {
        var cache = new FakeDistributedCache();
        var options = new RequestCachingOptions
        {
            Default = new RequestCacheEntryOptions { Duration = TimeSpan.FromMinutes(7) },
            Overrides =
            {
                ["Unrelated.Namespace"] = new RequestCacheEntryOptions { Duration = TimeSpan.FromSeconds(1) },
            },
        };

        var middleware = RequestCachingTestHelpers.CreateMiddleware(cache, options);
        var context = RequestCachingTestHelpers.CreateContext(new CacheableQuery { CacheKey = "d" });

        await middleware.InvokeAsync(context, ctx =>
        {
            ctx.Response = new PipelineResponse { Result = "ok" };
            return Task.CompletedTask;
        });

        Assert.Equal(TimeSpan.FromMinutes(7), cache.Sets[0].Options.AbsoluteExpirationRelativeToNow);
    }

    [Fact]
    public async Task SetCache_MapsDurationToAbsoluteExpirationRelativeToNow()
    {
        var cache = new FakeDistributedCache();
        var options = new RequestCachingOptions
        {
            Default = new RequestCacheEntryOptions { Duration = TimeSpan.FromSeconds(42) },
        };
        var middleware = RequestCachingTestHelpers.CreateMiddleware(cache, options);
        var context = RequestCachingTestHelpers.CreateContext(new CacheableQuery { CacheKey = "abs" });

        await middleware.InvokeAsync(context, ctx =>
        {
            ctx.Response = new PipelineResponse { Result = "ok" };
            return Task.CompletedTask;
        });

        Assert.Equal(TimeSpan.FromSeconds(42), cache.Sets[0].Options.AbsoluteExpirationRelativeToNow);
        Assert.Null(cache.Sets[0].Options.SlidingExpiration);
    }

    [Fact]
    public async Task InvokeAsync_OnCacheHit_DoesNotCallSet()
    {
        var cache = new FakeDistributedCache();
        var request = new CacheableQuery { CacheKey = "no-refresh" };
        var key = $"{typeof(CacheableQuery).FullName}:no-refresh";
        cache.Seed(key, System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(
            new PipelineResponse { Result = "cached" }));

        var middleware = RequestCachingTestHelpers.CreateMiddleware(cache);
        var context = RequestCachingTestHelpers.CreateContext(request);

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        Assert.Equal(0, cache.SetCount);
    }
}
