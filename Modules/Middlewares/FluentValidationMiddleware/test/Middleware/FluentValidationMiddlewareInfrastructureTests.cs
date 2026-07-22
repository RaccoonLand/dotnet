using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using RaccoonLand.Modules.Middlewares.FluentValidationMiddleware.Tests.Support;

namespace RaccoonLand.Modules.Middlewares.FluentValidationMiddleware.Tests.Middleware;

public sealed class FluentValidationMiddlewareInfrastructureTests
{
    [Fact]
    public async Task InvokeAsync_ResolvesValidatorsFromRequestServices()
    {
        var resolved = false;
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<SampleRequest>>(_ =>
        {
            resolved = true;
            return new RecordingValidator((_, _) => Task.FromResult(new ValidationResult()));
        });

        var middleware = FluentValidationTestHelpers.CreateMiddleware();
        var context = FluentValidationTestHelpers.CreateContext(services.BuildServiceProvider());

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        Assert.True(resolved);
    }

    [Fact]
    public async Task InvokeAsync_PassesCancellationTokenToValidateAsync()
    {
        using var cts = new CancellationTokenSource();
        CancellationToken seen = default;

        var validator = new RecordingValidator((_, ct) =>
        {
            seen = ct;
            return Task.FromResult(new ValidationResult());
        });

        var middleware = FluentValidationTestHelpers.CreateMiddleware();
        var context = FluentValidationTestHelpers.CreateContext(
            FluentValidationTestHelpers.ServicesWithValidators(validator),
            cancellationToken: cts.Token);

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        Assert.Equal(cts.Token, seen);
    }

    [Fact]
    public async Task InvokeAsync_WhenCanceled_StopsLaterValidators()
    {
        using var cts = new CancellationTokenSource();
        var secondRan = false;

        var first = new RecordingValidator(async (_, ct) =>
        {
            await cts.CancelAsync();
            ct.ThrowIfCancellationRequested();
            return new ValidationResult();
        });
        var second = new RecordingValidator((_, _) =>
        {
            secondRan = true;
            return Task.FromResult(new ValidationResult());
        });

        var middleware = FluentValidationTestHelpers.CreateMiddleware();
        var context = FluentValidationTestHelpers.CreateContext(
            FluentValidationTestHelpers.ServicesWithValidators(first, second),
            cancellationToken: cts.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => middleware.InvokeAsync(context, _ => Task.CompletedTask));

        Assert.False(secondRan);
    }

    [Fact]
    public async Task InvokeAsync_WhenContextNull_ThrowsArgumentNullException()
    {
        var middleware = FluentValidationTestHelpers.CreateMiddleware();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => middleware.InvokeAsync(null!, _ => Task.CompletedTask));
    }

    [Fact]
    public async Task InvokeAsync_WhenNextNull_ThrowsArgumentNullException()
    {
        var middleware = FluentValidationTestHelpers.CreateMiddleware();
        var context = FluentValidationTestHelpers.CreateContext(
            FluentValidationTestHelpers.ServicesWithValidators());

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => middleware.InvokeAsync(context, null!));
    }
}
