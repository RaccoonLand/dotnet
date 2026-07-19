using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;
using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;

namespace RaccoonLand.Modules.Middlewares.FluentValidationMiddleware;

/// <summary>
/// Pipeline middleware that runs the FluentValidation validators registered for the current request type
/// (<c>IValidator&lt;TRequest&gt;</c>). When there is no validator the request passes through untouched. When
/// validation fails the pipeline is short-circuited with a <see cref="PipelineResponse"/> error envelope: one
/// <see cref="PipelineMessage"/> per failure. Message text comes from <see cref="ValidationFailure.ErrorMessage"/>
/// as produced by the validator — localization belongs in the validator (for example via
/// <c>IMessageLocalization</c> injected into <c>AbstractValidator&lt;T&gt;</c>).
/// <para>
/// Validators run sequentially. Failures are aggregated across validators that complete; cancellation may stop
/// the loop before later validators run.
/// </para>
/// </summary>
public sealed class FluentValidationMiddleware(ILogger<FluentValidationMiddleware> logger) : IPipelineMiddleware
{
    public async Task InvokeAsync(PipelineContext context, PipelineDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var requestType = context.Request.GetType();
        var validatorType = typeof(IValidator<>).MakeGenericType(requestType);
        var validators = context.RequestServices.GetServices(validatorType).OfType<IValidator>().ToArray();

        if (validators.Length == 0)
        {
            await next(context);
            return;
        }

        var validationContext = new ValidationContext<object>(context.Request);
        var failures = new List<ValidationFailure>();

        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(validationContext, context.CancellationToken);
            if (!result.IsValid)
            {
                failures.AddRange(result.Errors);
            }
        }

        if (failures.Count == 0)
        {
            await next(context);
            return;
        }

        HandleValidationFailures(context, requestType, failures);
    }

    private void HandleValidationFailures(PipelineContext context, Type requestType, List<ValidationFailure> failures)
    {
        logger.LogDebug("Validation failed for {Request} with {Count} error(s).", requestType.Name, failures.Count);

        context.Response = new PipelineResponse
        {
            Errors = failures
                .Select(failure => new PipelineMessage(
                    string.IsNullOrEmpty(failure.ErrorCode) ? failure.PropertyName : failure.ErrorCode,
                    failure.ErrorMessage))
                .ToArray(),
        };
    }
}
