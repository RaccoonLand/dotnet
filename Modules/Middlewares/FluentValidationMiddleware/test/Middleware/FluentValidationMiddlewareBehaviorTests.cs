using FluentValidation;
using FluentValidation.Results;
using RaccoonLand.Modules.Middlewares.FluentValidationMiddleware.Tests.Support;

namespace RaccoonLand.Modules.Middlewares.FluentValidationMiddleware.Tests.Middleware;

public sealed class FluentValidationMiddlewareBehaviorTests
{
    [Fact]
    public async Task InvokeAsync_WhenNoValidator_CallsNext()
    {
        var middleware = FluentValidationTestHelpers.CreateMiddleware();
        var context = FluentValidationTestHelpers.CreateContext(
            FluentValidationTestHelpers.ServicesWithValidators());
        var nextCalled = false;

        await middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
        Assert.Null(context.Response);
    }

    [Fact]
    public async Task InvokeAsync_WhenValidatorSucceeds_CallsNext()
    {
        var validator = new RecordingValidator((_, _) =>
            Task.FromResult(new ValidationResult()));

        var middleware = FluentValidationTestHelpers.CreateMiddleware();
        var context = FluentValidationTestHelpers.CreateContext(
            FluentValidationTestHelpers.ServicesWithValidators(validator));
        var nextCalled = false;

        await middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
        Assert.Null(context.Response);
    }

    [Fact]
    public async Task InvokeAsync_WhenValidatorFails_SetsErrorResponseAndSkipsNext()
    {
        var validator = new RecordingValidator((_, _) =>
            Task.FromResult(new ValidationResult(
            [
                new ValidationFailure("Name", "required") { ErrorCode = "NAME_REQUIRED" },
            ])));

        var middleware = FluentValidationTestHelpers.CreateMiddleware();
        var context = FluentValidationTestHelpers.CreateContext(
            FluentValidationTestHelpers.ServicesWithValidators(validator));
        var nextCalled = false;

        await middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.False(nextCalled);
        Assert.NotNull(context.Response);
        Assert.Single(context.Response.Errors);
        Assert.Equal("NAME_REQUIRED", context.Response.Errors[0].Code);
        Assert.Equal("required", context.Response.Errors[0].Message);
    }

    [Fact]
    public async Task InvokeAsync_MultipleValidators_AggregatesFailures()
    {
        var first = new RecordingValidator((_, _) =>
            Task.FromResult(new ValidationResult(
            [
                new ValidationFailure("A", "a-fail") { ErrorCode = "A" },
            ])));
        var second = new RecordingValidator((_, _) =>
            Task.FromResult(new ValidationResult(
            [
                new ValidationFailure("B", "b-fail") { ErrorCode = "B" },
            ])));

        var middleware = FluentValidationTestHelpers.CreateMiddleware();
        var context = FluentValidationTestHelpers.CreateContext(
            FluentValidationTestHelpers.ServicesWithValidators(first, second));

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        Assert.Equal(2, context.Response!.Errors.Count);
        Assert.Equal(["A", "B"], context.Response.Errors.Select(e => e.Code).ToArray());
    }

    [Fact]
    public async Task InvokeAsync_ValidatorsRunSequentially()
    {
        var order = new List<string>();
        var first = new RecordingValidator(async (_, _) =>
        {
            order.Add("first-start");
            await Task.Yield();
            order.Add("first-end");
            return new ValidationResult();
        });
        var second = new RecordingValidator((_, _) =>
        {
            order.Add("second");
            return Task.FromResult(new ValidationResult());
        });

        var middleware = FluentValidationTestHelpers.CreateMiddleware();
        var context = FluentValidationTestHelpers.CreateContext(
            FluentValidationTestHelpers.ServicesWithValidators(first, second));

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        Assert.Equal(["first-start", "first-end", "second"], order);
    }
}
