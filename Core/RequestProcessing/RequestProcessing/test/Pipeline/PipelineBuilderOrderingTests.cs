using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;
using RaccoonLand.Core.RequestProcessing.Tests.Support;

namespace RaccoonLand.Core.RequestProcessing.Tests.Pipeline;

public sealed class PipelineBuilderOrderingTests
{
    [Fact]
    public async Task Build_RunsFirstRegisteredMiddlewareOutermost()
    {
        var order = new List<string>();
        var builder = new TestPipelineBuilder(
            new ServiceCollection().BuildServiceProvider(),
            _ =>
            {
                order.Add("terminal");
                return Task.CompletedTask;
            });

        builder.Use(next => async context =>
        {
            order.Add("A-before");
            await next(context);
            order.Add("A-after");
        });
        builder.Use(next => async context =>
        {
            order.Add("B-before");
            await next(context);
            order.Add("B-after");
        });

        await builder.Build()(CreateContext());

        Assert.Equal(
            ["A-before", "B-before", "terminal", "B-after", "A-after"],
            order);
    }

    [Fact]
    public void Use_Throws_WhenMiddlewareIsNull()
    {
        var builder = new TestPipelineBuilder(
            new ServiceCollection().BuildServiceProvider(),
            _ => Task.CompletedTask);

        Assert.Throws<ArgumentNullException>(() => builder.Use(null!));
    }

    [Fact]
    public void Constructor_Throws_WhenApplicationServicesIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new TestPipelineBuilder(null!, _ => Task.CompletedTask));
    }

    [Fact]
    public void Constructor_Throws_WhenTerminalIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new TestPipelineBuilder(new ServiceCollection().BuildServiceProvider(), null!));
    }

    private static PipelineContext CreateContext()
        => new(new DoSomethingCommand(), RequestKind.Command, new ServiceCollection().BuildServiceProvider());
}
