using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;
using RaccoonLand.Modules.Security.Authorization.Abstractions;
using RaccoonLand.Modules.Security.Authorization.Middlewares.Tests.Support;

namespace RaccoonLand.Modules.Security.Authorization.Middlewares.Tests.Middleware;

public sealed class AuthorizationMiddlewareBehaviorTests
{
    [Fact]
    public async Task InvokeAsync_WhenAllowed_CallsNextExactlyOnce()
    {
        var provider = new FakeAuthorizationProvider(AuthorizationDecision.Allow());
        var context = AuthorizationMiddlewareTestHelpers.CreateContext(provider);
        var middleware = new AuthorizationMiddleware();

        var nextCallCount = 0;
        await middleware.InvokeAsync(context, _ =>
        {
            nextCallCount++;
            return Task.CompletedTask;
        });

        Assert.Equal(1, nextCallCount);
        Assert.Null(context.Response);
    }

    [Fact]
    public async Task InvokeAsync_WhenDenied_DoesNotCallNext()
    {
        var provider = new FakeAuthorizationProvider(AuthorizationDecision.Deny());
        var context = AuthorizationMiddlewareTestHelpers.CreateContext(provider);
        var middleware = new AuthorizationMiddleware();

        var nextCallCount = 0;
        await middleware.InvokeAsync(context, _ =>
        {
            nextCallCount++;
            return Task.CompletedTask;
        });

        Assert.Equal(0, nextCallCount);
        Assert.NotNull(context.Response);
    }

    [Fact]
    public async Task InvokeAsync_WhenUnauthenticated_DoesNotCallNext()
    {
        var provider = new FakeAuthorizationProvider(AuthorizationDecision.Unauthenticated());
        var context = AuthorizationMiddlewareTestHelpers.CreateContext(provider);
        var middleware = new AuthorizationMiddleware();

        var nextCallCount = 0;
        await middleware.InvokeAsync(context, _ =>
        {
            nextCallCount++;
            return Task.CompletedTask;
        });

        Assert.Equal(0, nextCallCount);
        Assert.NotNull(context.Response);
    }

    [Fact]
    public async Task InvokeAsync_CallsProviderWithConcreteFullNameAndCancellationToken()
    {
        var provider = new FakeAuthorizationProvider(AuthorizationDecision.Allow());
        using var cts = new CancellationTokenSource();
        var context = AuthorizationMiddlewareTestHelpers.CreateContext(
            provider,
            cancellationToken: cts.Token);
        var middleware = new AuthorizationMiddleware();

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        Assert.Equal(1, provider.CallCount);
        Assert.NotNull(provider.LastContext);
        Assert.Equal(typeof(SampleRequest).FullName, provider.LastContext!.RequestName);
        Assert.Equal(cts.Token, provider.LastCancellationToken);
    }
}
