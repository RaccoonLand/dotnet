using RaccoonLand.Core.Domain.Exceptions;
using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;
using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;
using RaccoonLand.Modules.Middlewares.ExceptionHandlingMiddleware.Tests.Support;

namespace RaccoonLand.Modules.Middlewares.ExceptionHandlingMiddleware.Tests.Middleware;

public sealed class ExceptionHandlingMiddlewareHandlerTests
{
    [Fact]
    public async Task InvokeAsync_WhenHandlerReturnsTrue_ConsumesException()
    {
        var options = new ExceptionHandlingOptions();
        options.On<InvalidOperationException>(async (ctx, _) =>
        {
            ctx.Response = new PipelineResponse
            {
                Errors = [new PipelineMessage("H", "handled")],
            };
            return true;
        });

        var middleware = ExceptionHandlingTestHelpers.CreateMiddleware(options);
        var context = ExceptionHandlingTestHelpers.CreateContext();

        await middleware.InvokeAsync(context, _ => throw new InvalidOperationException("boom"));

        Assert.NotNull(context.Response);
        Assert.Equal("H", context.Response.Errors[0].Code);
    }

    [Fact]
    public async Task InvokeAsync_WhenHandlerReturnsFalse_InvokesNextHandler()
    {
        var order = new List<string>();
        var options = new ExceptionHandlingOptions();
        options.On<InvalidOperationException>((_, _) =>
        {
            order.Add("first");
            return Task.FromResult(false);
        });
        options.On<InvalidOperationException>(async (ctx, _) =>
        {
            order.Add("second");
            ctx.Response = new PipelineResponse
            {
                Errors = [new PipelineMessage("S", "second")],
            };
            return true;
        });

        var middleware = ExceptionHandlingTestHelpers.CreateMiddleware(options);
        var context = ExceptionHandlingTestHelpers.CreateContext();

        await middleware.InvokeAsync(context, _ => throw new InvalidOperationException("boom"));

        Assert.Equal(["first", "second"], order);
        Assert.Equal("S", context.Response!.Errors[0].Code);
    }

    [Fact]
    public async Task InvokeAsync_HandlersRunInRegistrationOrder()
    {
        var order = new List<string>();
        var options = new ExceptionHandlingOptions();
        options.On<Exception>((_, _) =>
        {
            order.Add("a");
            return Task.FromResult(false);
        });
        options.On<Exception>((_, _) =>
        {
            order.Add("b");
            return Task.FromResult(false);
        });
        options.On<Exception>(async (ctx, _) =>
        {
            order.Add("c");
            ctx.Response = new PipelineResponse();
            return true;
        });

        var middleware = ExceptionHandlingTestHelpers.CreateMiddleware(options);
        var context = ExceptionHandlingTestHelpers.CreateContext();

        await middleware.InvokeAsync(context, _ => throw new InvalidOperationException());

        Assert.Equal(["a", "b", "c"], order);
    }

    [Fact]
    public async Task InvokeAsync_BaseTypeHandler_MatchesDerivedException()
    {
        var options = new ExceptionHandlingOptions();
        options.On<Exception>(async (ctx, _) =>
        {
            ctx.Response = new PipelineResponse
            {
                Errors = [new PipelineMessage("BASE", "matched")],
            };
            return true;
        });

        var middleware = ExceptionHandlingTestHelpers.CreateMiddleware(options);
        var context = ExceptionHandlingTestHelpers.CreateContext();

        await middleware.InvokeAsync(context, _ => throw new ArgumentException("derived"));

        Assert.Equal("BASE", context.Response!.Errors[0].Code);
    }

    [Fact]
    public void On_WhenHandlerIsNull_ThrowsArgumentNullException()
    {
        var options = new ExceptionHandlingOptions();

        Assert.Throws<ArgumentNullException>(() =>
            options.On<InvalidOperationException>(null!));
    }

    [Fact]
    public async Task InvokeAsync_WhenHandlerReturnsFalseForDomainException_UsesBuiltInMapping()
    {
        var options = new ExceptionHandlingOptions();
        options.On<DomainException>((_, _) => Task.FromResult(false));

        var middleware = ExceptionHandlingTestHelpers.CreateMiddleware(options);
        var context = ExceptionHandlingTestHelpers.CreateContext();

        await middleware.InvokeAsync(
            context,
            _ => throw new DomainException("CODE", "TEMPLATE_KEY"));

        Assert.NotNull(context.Response);
        Assert.Equal("CODE", context.Response.Errors[0].Code);
        Assert.Equal("TEMPLATE_KEY", context.Response.Errors[0].Message);
    }
}
