using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;
using RaccoonLand.Core.RequestProcessing.Abstractions.Tests.Support;

namespace RaccoonLand.Core.RequestProcessing.Abstractions.Tests.Pipeline;

public sealed class PipelineBuilderExtensionsUseTests
{
    [Fact]
    public async Task Use_InvokesNext_AndContinuesToTerminal()
    {
        var terminalRan = false;
        var builder = new TestPipelineBuilder(
            new ServiceCollection().BuildServiceProvider(),
            _ =>
            {
                terminalRan = true;
                return Task.CompletedTask;
            });

        builder.Use(async (_, next) => await next());

        await builder.Build()(CreateContext());

        Assert.True(terminalRan);
    }

    [Fact]
    public async Task Use_PassesSameContextInstance_BetweenMiddlewareAndTerminal()
    {
        PipelineContext? seenInMiddleware = null;
        PipelineContext? seenInTerminal = null;

        var builder = new TestPipelineBuilder(
            new ServiceCollection().BuildServiceProvider(),
            context =>
            {
                seenInTerminal = context;
                return Task.CompletedTask;
            });

        builder.Use(async (context, next) =>
        {
            seenInMiddleware = context;
            await next();
        });

        var context = CreateContext();
        await builder.Build()(context);

        Assert.Same(context, seenInMiddleware);
        Assert.Same(context, seenInTerminal);
    }

    [Fact]
    public async Task Use_ShortCircuits_WhenNextIsNotCalled()
    {
        var terminalRan = false;
        var builder = new TestPipelineBuilder(
            new ServiceCollection().BuildServiceProvider(),
            _ =>
            {
                terminalRan = true;
                return Task.CompletedTask;
            });

        builder.Use((_, _) =>
        {
            // short-circuit
            return Task.CompletedTask;
        });

        await builder.Build()(CreateContext());

        Assert.False(terminalRan);
    }

    [Fact]
    public void Use_Throws_WhenMiddlewareIsNull()
    {
        var builder = new TestPipelineBuilder(new ServiceCollection().BuildServiceProvider());

        Assert.Throws<ArgumentNullException>(() =>
            PipelineBuilderExtensions.Use(builder, null!));
    }

    private static PipelineContext CreateContext()
        => new(new SampleRequest(), RequestKind.Command, new ServiceCollection().BuildServiceProvider());
}
