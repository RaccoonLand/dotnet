using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;

namespace RaccoonLand.Core.Hosting.Worker;

/// <summary>The outcome of a background dispatch. The full <see cref="PipelineResponse"/> envelope is preserved.</summary>
public sealed record WorkerDispatchResult
{
    public required bool IsSuccess { get; init; }

    public PipelineResponse Response { get; init; } = new();
}
