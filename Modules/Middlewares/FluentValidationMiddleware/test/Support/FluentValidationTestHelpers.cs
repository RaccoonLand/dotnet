using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;

namespace RaccoonLand.Modules.Middlewares.FluentValidationMiddleware.Tests.Support;

internal sealed class SampleRequest : IRequest
{
    public string? Name { get; init; }
}

internal sealed class RecordingValidator : IValidator<SampleRequest>
{
    private readonly Func<IValidationContext, CancellationToken, Task<ValidationResult>> _validate;

    public RecordingValidator(
        Func<IValidationContext, CancellationToken, Task<ValidationResult>> validate)
    {
        _validate = validate;
    }

    public bool CanValidateInstancesOfType(Type type) => type == typeof(SampleRequest);

    public IValidatorDescriptor CreateDescriptor() => throw new NotSupportedException();

    public ValidationResult Validate(IValidationContext context)
        => ValidateAsync(context, CancellationToken.None).GetAwaiter().GetResult();

    public Task<ValidationResult> ValidateAsync(
        IValidationContext context,
        CancellationToken cancellation = default)
        => _validate(context, cancellation);

    public ValidationResult Validate(SampleRequest instance)
        => ValidateAsync(instance, CancellationToken.None).GetAwaiter().GetResult();

    public Task<ValidationResult> ValidateAsync(
        SampleRequest instance,
        CancellationToken cancellation = default)
        => ValidateAsync(new ValidationContext<SampleRequest>(instance), cancellation);
}

internal static class FluentValidationTestHelpers
{
    public static FluentValidationMiddleware CreateMiddleware()
        => new(NullLogger<FluentValidationMiddleware>.Instance);

    public static PipelineContext CreateContext(
        IServiceProvider services,
        SampleRequest? request = null,
        CancellationToken cancellationToken = default)
        => new(
            request ?? new SampleRequest(),
            RequestKind.Command,
            services,
            cancellationToken);

    public static IServiceProvider ServicesWithValidators(params IValidator<SampleRequest>[] validators)
    {
        var services = new ServiceCollection();
        foreach (var validator in validators)
        {
            services.AddSingleton<IValidator<SampleRequest>>(validator);
        }

        return services.BuildServiceProvider();
    }
}
