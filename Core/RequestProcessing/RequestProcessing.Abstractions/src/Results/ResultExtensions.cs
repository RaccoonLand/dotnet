using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;

namespace RaccoonLand.Core.RequestProcessing.Abstractions.Results;

/// <summary>
/// Maps an endpoint <see cref="Result"/>/<see cref="Result{TValue}"/> to the untyped pipeline
/// <see cref="PipelineResponse"/> envelope. A pipeline terminal calls this to publish a handler's outcome
/// onto <c>PipelineContext.Response</c>.
/// </summary>
public static class ResultExtensions
{
    /// <summary>Builds a <see cref="PipelineResponse"/> from a payload‑less <see cref="Result"/>.</summary>
    public static PipelineResponse ToPipelineResponse(this Result result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new PipelineResponse
        {
            Result = null,
            Errors = result.Errors,
            Warnings = result.Warnings,
        };
    }

    /// <summary>Builds a <see cref="PipelineResponse"/> from a <see cref="Result{TValue}"/>.</summary>
    public static PipelineResponse ToPipelineResponse<TValue>(this Result<TValue> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new PipelineResponse
        {
            Result = result.IsSuccess ? result.Value : null,
            Errors = result.Errors,
            Warnings = result.Warnings,
        };
    }
}
