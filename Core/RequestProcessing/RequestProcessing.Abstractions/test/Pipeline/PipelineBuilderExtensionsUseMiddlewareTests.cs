using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;
using RaccoonLand.Core.RequestProcessing.Abstractions.Tests.Support;

namespace RaccoonLand.Core.RequestProcessing.Abstractions.Tests.Pipeline;

public sealed class PipelineBuilderExtensionsUseMiddlewareTests
{
    [Fact]
    public async Task UseMiddleware_ResolvesFromApplicationServices_AtConfigurationTime()
    {
        var resolveCount = 0;
        var services = new ServiceCollection();
        services.AddSingleton<RecordingMiddleware>(_ =>
        {
            resolveCount++;
            return new RecordingMiddleware();
        });
        var provider = services.BuildServiceProvider();

        var builder = new TestPipelineBuilder(provider, _ => Task.CompletedTask);
        Assert.Equal(0, resolveCount);

        builder.UseMiddleware<RecordingMiddleware>();
        Assert.Equal(1, resolveCount);

        await builder.Build()(CreateContext(provider));
        await builder.Build()(CreateContext(provider));

        Assert.Equal(1, resolveCount);
    }

    [Fact]
    public void UseMiddleware_Throws_WhenMiddlewareIsNotRegistered()
    {
        var builder = new TestPipelineBuilder(new ServiceCollection().BuildServiceProvider());

        Assert.Throws<InvalidOperationException>(() =>
            builder.UseMiddleware<RecordingMiddleware>());
    }

    [Fact]
    public async Task UseMiddleware_ExecutesInvokeAsync_BridgesContext_AndCallsNext()
    {
        var sequence = new List<string>();
        var services = new ServiceCollection();
        var middleware = new SequencingMiddleware(sequence);
        services.AddSingleton(middleware);
        var provider = services.BuildServiceProvider();

        var builder = new TestPipelineBuilder(provider, context =>
        {
            sequence.Add("terminal");
            Assert.Same(middleware.LastContext, context);
            return Task.CompletedTask;
        });
        builder.UseMiddleware<SequencingMiddleware>();

        var context = CreateContext(provider);
        await builder.Build()(context);

        Assert.Equal(1, middleware.InvokeCount);
        Assert.Same(context, middleware.LastContext);
        Assert.NotNull(middleware.LastNext);
        Assert.Equal(["mw-before", "terminal", "mw-after"], sequence);
    }

    [Fact]
    public async Task UseMiddleware_ShortCircuits_WhenNextIsNotCalled()
    {
        var services = new ServiceCollection();
        var middleware = new ShortCircuitMiddleware();
        services.AddSingleton(middleware);
        var provider = services.BuildServiceProvider();

        var terminalRan = false;
        var builder = new TestPipelineBuilder(provider, _ =>
        {
            terminalRan = true;
            return Task.CompletedTask;
        });
        builder.UseMiddleware<ShortCircuitMiddleware>();

        var context = CreateContext(provider);
        await builder.Build()(context);

        Assert.Equal(1, middleware.InvokeCount);
        Assert.Same(context, middleware.LastContext);
        Assert.False(terminalRan);
    }

    private static PipelineContext CreateContext(IServiceProvider services)
        => new(new SampleRequest(), RequestKind.Command, services);

    private class RecordingMiddleware : IPipelineMiddleware
    {
        public int InvokeCount { get; protected set; }

        public PipelineContext? LastContext { get; protected set; }

        public PipelineDelegate? LastNext { get; protected set; }

        public virtual Task InvokeAsync(PipelineContext context, PipelineDelegate next)
        {
            InvokeCount++;
            LastContext = context;
            LastNext = next;
            return next(context);
        }
    }

    private sealed class SequencingMiddleware(List<string> sequence) : RecordingMiddleware
    {
        public override async Task InvokeAsync(PipelineContext context, PipelineDelegate next)
        {
            InvokeCount++;
            LastContext = context;
            LastNext = next;
            sequence.Add("mw-before");
            await next(context);
            sequence.Add("mw-after");
        }
    }

    private sealed class ShortCircuitMiddleware : RecordingMiddleware
    {
        public override Task InvokeAsync(PipelineContext context, PipelineDelegate next)
        {
            InvokeCount++;
            LastContext = context;
            LastNext = next;
            return Task.CompletedTask;
        }
    }
}
