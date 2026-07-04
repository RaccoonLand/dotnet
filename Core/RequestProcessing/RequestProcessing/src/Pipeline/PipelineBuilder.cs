using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;

namespace RaccoonLand.Core.RequestProcessing.Pipeline;

/// <summary>
/// Default <see cref="IPipelineBuilder"/>. The build algorithm is identical to ASP.NET Core's
/// application builder: the terminal delegate is wrapped by each component in reverse order so that
/// the first-registered component becomes the outermost one.
/// </summary>
public abstract class PipelineBuilder : IPipelineBuilder
{
    private readonly List<Func<PipelineDelegate, PipelineDelegate>> _components = [];
    private readonly PipelineDelegate _terminal;

    protected PipelineBuilder(IServiceProvider applicationServices, PipelineDelegate terminal)
    {
        ArgumentNullException.ThrowIfNull(applicationServices);
        ArgumentNullException.ThrowIfNull(terminal);

        ApplicationServices = applicationServices;
        _terminal = terminal;
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
