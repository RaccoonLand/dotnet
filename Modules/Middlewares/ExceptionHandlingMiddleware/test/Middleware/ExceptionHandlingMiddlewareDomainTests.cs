using Microsoft.Extensions.DependencyInjection;
using RaccoonLand.Core.Domain.Exceptions;
using RaccoonLand.Modules.MessageLocalization.Abstraction;
using RaccoonLand.Modules.Middlewares.ExceptionHandlingMiddleware.Tests.Support;

namespace RaccoonLand.Modules.Middlewares.ExceptionHandlingMiddleware.Tests.Middleware;

public sealed class ExceptionHandlingMiddlewareDomainTests
{
    [Fact]
    public async Task InvokeAsync_DomainExceptionWithoutLocalizer_MapsErrorsDirectly()
    {
        var middleware = ExceptionHandlingTestHelpers.CreateMiddleware(new ExceptionHandlingOptions());
        var context = ExceptionHandlingTestHelpers.CreateContext();

        await middleware.InvokeAsync(
            context,
            _ => throw new DomainException("E1", "msg-key", 10));

        Assert.NotNull(context.Response);
        Assert.Single(context.Response.Errors);
        Assert.Equal("E1", context.Response.Errors[0].Code);
        Assert.Equal("msg-key", context.Response.Errors[0].Message);
    }

    [Fact]
    public async Task InvokeAsync_DomainExceptionWithLocalizer_UsesLocalizedMessage()
    {
        var localizer = new FakeMessageLocalization();
        var services = new ServiceCollection();
        services.AddSingleton<IMessageLocalization>(localizer);

        var middleware = ExceptionHandlingTestHelpers.CreateMiddleware(new ExceptionHandlingOptions());
        var context = ExceptionHandlingTestHelpers.CreateContext(services.BuildServiceProvider());

        await middleware.InvokeAsync(
            context,
            _ => throw new DomainException("E1", "msg-key", 10, "x"));

        Assert.Equal("LOC:msg-key:10,x", context.Response!.Errors[0].Message);
        Assert.Single(localizer.Calls);
        Assert.Equal("msg-key", localizer.Calls[0].Template);
    }

    [Fact]
    public async Task InvokeAsync_DomainExceptionWithLocalizer_ForwardsAllParameters()
    {
        var localizer = new FakeMessageLocalization();
        var services = new ServiceCollection();
        services.AddSingleton<IMessageLocalization>(localizer);

        var middleware = ExceptionHandlingTestHelpers.CreateMiddleware(new ExceptionHandlingOptions());
        var context = ExceptionHandlingTestHelpers.CreateContext(services.BuildServiceProvider());

        await middleware.InvokeAsync(
            context,
            _ => throw new DomainException("E1", "BETWEEN", 10, null, "x"));

        Assert.Single(localizer.Calls);
        Assert.Equal("BETWEEN", localizer.Calls[0].Template);
        Assert.Equal(3, localizer.Calls[0].Parameters.Length);
        Assert.Equal(10, localizer.Calls[0].Parameters[0]);
        Assert.Null(localizer.Calls[0].Parameters[1]);
        Assert.Equal("x", localizer.Calls[0].Parameters[2]);
        Assert.Equal("LOC:BETWEEN:10,,x", context.Response!.Errors[0].Message);
    }

    [Fact]
    public async Task InvokeAsync_DomainExceptionWithMultipleErrors_MapsEachErrorThroughLocalizer()
    {
        var localizer = new FakeMessageLocalization();
        var services = new ServiceCollection();
        services.AddSingleton<IMessageLocalization>(localizer);

        var middleware = ExceptionHandlingTestHelpers.CreateMiddleware(new ExceptionHandlingOptions());
        var context = ExceptionHandlingTestHelpers.CreateContext(services.BuildServiceProvider());

        await middleware.InvokeAsync(
            context,
            _ => throw new DomainException(
                new DomainError("E1", "KEY_ONE", 1, "a"),
                new DomainError("E2", "KEY_TWO", null, 2)));

        Assert.Equal(2, context.Response!.Errors.Count);
        Assert.Equal("E1", context.Response.Errors[0].Code);
        Assert.Equal("LOC:KEY_ONE:1,a", context.Response.Errors[0].Message);
        Assert.Equal("E2", context.Response.Errors[1].Code);
        Assert.Equal("LOC:KEY_TWO:,2", context.Response.Errors[1].Message);

        Assert.Equal(2, localizer.Calls.Count);
        Assert.Equal("KEY_ONE", localizer.Calls[0].Template);
        Assert.Equal([1, "a"], localizer.Calls[0].Parameters);
        Assert.Equal("KEY_TWO", localizer.Calls[1].Template);
        Assert.Equal(2, localizer.Calls[1].Parameters.Length);
        Assert.Null(localizer.Calls[1].Parameters[0]);
        Assert.Equal(2, localizer.Calls[1].Parameters[1]);
    }

    [Fact]
    public async Task InvokeAsync_UnknownException_Rethrows()
    {
        var middleware = ExceptionHandlingTestHelpers.CreateMiddleware(new ExceptionHandlingOptions());
        var context = ExceptionHandlingTestHelpers.CreateContext();
        var original = new InvalidOperationException("unknown");

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.InvokeAsync(context, _ => throw original));

        Assert.Same(original, thrown);
        Assert.Null(context.Response);
    }
}
