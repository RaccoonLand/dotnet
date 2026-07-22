using FluentValidation.Results;
using RaccoonLand.Modules.Middlewares.FluentValidationMiddleware.Tests.Support;

namespace RaccoonLand.Modules.Middlewares.FluentValidationMiddleware.Tests.Middleware;

public sealed class FluentValidationMiddlewareMappingTests
{
    [Fact]
    public async Task InvokeAsync_WhenErrorCodePresent_UsesErrorCodeAsCode()
    {
        var validator = new RecordingValidator((_, _) =>
            Task.FromResult(new ValidationResult(
            [
                new ValidationFailure("Name", "msg") { ErrorCode = "ERR_CODE" },
            ])));

        var middleware = FluentValidationTestHelpers.CreateMiddleware();
        var context = FluentValidationTestHelpers.CreateContext(
            FluentValidationTestHelpers.ServicesWithValidators(validator));

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        Assert.Equal("ERR_CODE", context.Response!.Errors[0].Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task InvokeAsync_WhenErrorCodeMissing_FallsBackToPropertyName(string? errorCode)
    {
        var validator = new RecordingValidator((_, _) =>
            Task.FromResult(new ValidationResult(
            [
                new ValidationFailure("Name", "msg") { ErrorCode = errorCode! },
            ])));

        var middleware = FluentValidationTestHelpers.CreateMiddleware();
        var context = FluentValidationTestHelpers.CreateContext(
            FluentValidationTestHelpers.ServicesWithValidators(validator));

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        Assert.Equal("Name", context.Response!.Errors[0].Code);
    }

    [Fact]
    public async Task InvokeAsync_PassesErrorMessageUnchanged()
    {
        const string message = "exact message text";
        var validator = new RecordingValidator((_, _) =>
            Task.FromResult(new ValidationResult(
            [
                new ValidationFailure("Name", message) { ErrorCode = "E" },
            ])));

        var middleware = FluentValidationTestHelpers.CreateMiddleware();
        var context = FluentValidationTestHelpers.CreateContext(
            FluentValidationTestHelpers.ServicesWithValidators(validator));

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        Assert.Equal(message, context.Response!.Errors[0].Message);
    }
}
