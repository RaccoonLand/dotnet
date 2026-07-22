using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using RaccoonLand.Core.Hosting.AspNetCore.HttpExecutionContext;
using ExecutionContextType = RaccoonLand.Core.Hosting.AspNetCore.HttpExecutionContext.HttpExecutionContext;

namespace RaccoonLand.Core.Hosting.AspNetCore.Tests.HttpExecutionContext;

public sealed class HttpExecutionContextMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ResolvesContextFromRequestServices_PopulatesBeforeNext()
    {
        var services = new ServiceCollection();
        services.AddScoped<ExecutionContextType>();
        var provider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext();
        using var scope = provider.CreateScope();
        httpContext.RequestServices = scope.ServiceProvider;
        httpContext.Request.Headers["X-Correlation-Id"] = "corr-42";

        ExecutionContextType? seenInNext = null;
        var nextCount = 0;
        RequestDelegate next = ctx =>
        {
            nextCount++;
            seenInNext = ctx.RequestServices.GetRequiredService<ExecutionContextType>();
            Assert.Equal("corr-42", seenInNext.CorrelationId);
            Assert.True(seenInNext.IsAvailable);
            return Task.CompletedTask;
        };

        var middleware = new HttpExecutionContextMiddleware(next);
        var options = Options.Create(new HttpExecutionContextOptions
        {
            CorrelationIdHeader = "X-Correlation-Id",
            GenerateCorrelationIdWhenMissing = false,
            EchoCorrelationIdInResponse = false,
        });

        await middleware.InvokeAsync(httpContext, options);

        Assert.Equal(1, nextCount);
        Assert.NotNull(seenInNext);
        Assert.Equal("corr-42", seenInNext!.CorrelationId);
    }

    [Fact]
    public async Task InvokeAsync_DoesNotLeakState_AcrossSeparateScopes()
    {
        var services = new ServiceCollection();
        services.AddScoped<ExecutionContextType>();
        var provider = services.BuildServiceProvider();

        var middleware = new HttpExecutionContextMiddleware(_ => Task.CompletedTask);
        var options = Options.Create(new HttpExecutionContextOptions
        {
            CorrelationIdHeader = "X-Correlation-Id",
            GenerateCorrelationIdWhenMissing = false,
            EchoCorrelationIdInResponse = false,
        });

        ExecutionContextType firstContext;
        using (var scope1 = provider.CreateScope())
        {
            var http1 = new DefaultHttpContext { RequestServices = scope1.ServiceProvider };
            http1.Request.Headers["X-Correlation-Id"] = "first";
            await middleware.InvokeAsync(http1, options);
            firstContext = scope1.ServiceProvider.GetRequiredService<ExecutionContextType>();
            Assert.Equal("first", firstContext.CorrelationId);
        }

        using (var scope2 = provider.CreateScope())
        {
            var http2 = new DefaultHttpContext { RequestServices = scope2.ServiceProvider };
            http2.Request.Headers["X-Correlation-Id"] = "second";
            await middleware.InvokeAsync(http2, options);
            var secondContext = scope2.ServiceProvider.GetRequiredService<ExecutionContextType>();
            Assert.Equal("second", secondContext.CorrelationId);
            Assert.NotSame(firstContext, secondContext);
        }
    }
}
