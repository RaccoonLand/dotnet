using RaccoonLand.Modules.Security.Authorization.Abstractions;
using RaccoonLand.Modules.Security.Authorization.Middlewares.Tests.Support;

namespace RaccoonLand.Modules.Security.Authorization.Middlewares.Tests.Middleware;

public sealed class AuthorizationMiddlewareMessageTests
{
    [Fact]
    public async Task InvokeAsync_WhenUnauthenticated_ProducesAuthenticationRequired401()
    {
        var provider = new FakeAuthorizationProvider(AuthorizationDecision.Unauthenticated());
        var context = AuthorizationMiddlewareTestHelpers.CreateContext(provider);
        var middleware = new AuthorizationMiddleware();

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        Assert.NotNull(context.Response);
        Assert.Equal(401, context.Response!.StatusHint);
        var error = Assert.Single(context.Response.Errors);
        Assert.Equal(AuthorizationMessageTemplates.AuthenticationRequired, error.Code);
        Assert.Equal(AuthorizationMessageTemplates.AuthenticationRequired, error.Message);
    }

    [Fact]
    public async Task InvokeAsync_WhenDenied_ProducesAccessDenied403()
    {
        var provider = new FakeAuthorizationProvider(AuthorizationDecision.Deny());
        var context = AuthorizationMiddlewareTestHelpers.CreateContext(provider);
        var middleware = new AuthorizationMiddleware();

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        Assert.NotNull(context.Response);
        Assert.Equal(403, context.Response!.StatusHint);
        var error = Assert.Single(context.Response.Errors);
        Assert.Equal(AuthorizationMessageTemplates.AccessDenied, error.Code);
        Assert.Equal(AuthorizationMessageTemplates.AccessDenied, error.Message);
    }

    [Fact]
    public async Task InvokeAsync_WhenAllowed_ProducesNoErrorResponse()
    {
        var provider = new FakeAuthorizationProvider(AuthorizationDecision.Allow());
        var context = AuthorizationMiddlewareTestHelpers.CreateContext(provider);
        var middleware = new AuthorizationMiddleware();

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        Assert.Null(context.Response);
    }

    [Fact]
    public async Task InvokeAsync_WhenLocalizerRegistered_ResolvesMessageThroughLocalizer()
    {
        var provider = new FakeAuthorizationProvider(AuthorizationDecision.Deny());
        var localizer = new FakeMessageLocalization();
        var context = AuthorizationMiddlewareTestHelpers.CreateContext(provider, localizer);
        var middleware = new AuthorizationMiddleware();

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        Assert.NotNull(context.Response);
        var error = Assert.Single(context.Response!.Errors);
        Assert.Equal(AuthorizationMessageTemplates.AccessDenied, error.Code);
        Assert.Equal("LOC:" + AuthorizationMessageTemplates.AccessDenied, error.Message);
        Assert.Equal([AuthorizationMessageTemplates.AccessDenied], localizer.ResolvedTemplates);
    }

    [Fact]
    public async Task InvokeAsync_WhenNoLocalizer_UsesTemplateKeyAsMessage()
    {
        var provider = new FakeAuthorizationProvider(AuthorizationDecision.Unauthenticated());
        var context = AuthorizationMiddlewareTestHelpers.CreateContext(provider);
        var middleware = new AuthorizationMiddleware();

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        var error = Assert.Single(context.Response!.Errors);
        Assert.Equal(error.Code, error.Message);
    }
}
