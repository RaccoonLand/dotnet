using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;

namespace RaccoonLand.Core.Hosting.Worker;

/// <summary>
/// Default worker interpretation: success when the envelope has no errors; failure when
/// <see cref="PipelineResponse.Errors"/> is non-empty. The full envelope is always preserved on the result.
/// </summary>
public sealed class DefaultPipelineResponseHandler : IPipelineResponseHandler
{
    public WorkerDispatchResult Handle(PipelineResponse? response)
    {
        var envelope = response ?? new PipelineResponse();
        return new WorkerDispatchResult
        {
            IsSuccess = envelope.Errors.Count == 0,
            Response = envelope,
        };
    }
}
