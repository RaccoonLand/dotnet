using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;

namespace RaccoonLand.Core.RequestProcessing.Abstractions.Tests.Support;

/// <summary>
/// Minimal <see cref="IPipelineBuilder"/> for testing extension methods in isolation.
/// Uses the same reverse-wrap composition as the production builder.
/// </summary>
internal sealed class TestPipelineBuilder : IPipelineBuilder
{
    private readonly List<Func<PipelineDelegate, PipelineDelegate>> _components = [];
    private readonly PipelineDelegate _terminal;

    public TestPipelineBuilder(IServiceProvider applicationServices, PipelineDelegate? terminal = null)
    {
        ApplicationServices = applicationServices;
        _terminal = terminal ?? (_ => Task.CompletedTask);
    }

    public IServiceProvider ApplicationServices { get; }

    public IPipelineBuilder Use(Func<PipelineDelegate, PipelineDelegate> middleware)
    {
        ArgumentNullException.ThrowIfNull(middleware);
        _components.Add(middleware);
        return this;
    }

    public PipelineDelegate Build()
    {
        var app = _terminal;
        for (var i = _components.Count - 1; i >= 0; i--)
        {
            app = _components[i](app);
        }

        return app;
    }
}
