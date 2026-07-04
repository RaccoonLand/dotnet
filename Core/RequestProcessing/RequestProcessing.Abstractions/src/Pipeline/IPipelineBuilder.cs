namespace RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;

/// <summary>
/// Builds a pipeline from a set of middleware components, mirroring ASP.NET Core's
/// <c>IApplicationBuilder</c>. Components added first run first (outermost).
/// </summary>
public interface IPipelineBuilder
{
    /// <summary>The application (root) service provider.</summary>
    IServiceProvider ApplicationServices { get; }

    /// <summary>Adds a middleware component to the pipeline.</summary>
    IPipelineBuilder Use(Func<PipelineDelegate, PipelineDelegate> middleware);

    /// <summary>Composes all components (and the terminal endpoint invoker) into a single delegate.</summary>
    PipelineDelegate Build();
}
