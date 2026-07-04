using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;

namespace RaccoonLand.Core.RequestProcessing.Pipeline;

/// <summary>
/// Holds the two compiled pipeline delegates after the application has configured them. The dispatcher
/// reads from here at runtime to pick the right pipeline for each request.
/// </summary>
public sealed class CompiledPipelines
{
    public PipelineDelegate? Command { get; set; }

    public PipelineDelegate? Query { get; set; }
}
