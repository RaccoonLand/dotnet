using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;

namespace RaccoonLand.Core.RequestProcessing.Pipeline;

/// <summary>
/// Holds the two compiled pipeline delegates after the application has configured them. The dispatcher
/// reads from here at runtime to pick the right pipeline for each request. Immutable after construction —
/// both delegates are required and validated non-null at construction.
/// </summary>
public sealed class CompiledPipelines
{
    public CompiledPipelines(PipelineDelegate command, PipelineDelegate query)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(query);
        Command = command;
        Query = query;
    }

    /// <summary>The compiled command pipeline.</summary>
    public PipelineDelegate Command { get; }

    /// <summary>The compiled query pipeline.</summary>
    public PipelineDelegate Query { get; }
}
