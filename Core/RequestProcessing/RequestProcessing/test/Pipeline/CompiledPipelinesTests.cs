using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;
using RaccoonLand.Core.RequestProcessing.Pipeline;

namespace RaccoonLand.Core.RequestProcessing.Tests.Pipeline;

public sealed class CompiledPipelinesTests
{
    [Fact]
    public void Constructor_Throws_WhenCommandIsNull()
    {
        PipelineDelegate query = _ => Task.CompletedTask;

        Assert.Throws<ArgumentNullException>(() => new CompiledPipelines(null!, query));
    }

    [Fact]
    public void Constructor_Throws_WhenQueryIsNull()
    {
        PipelineDelegate command = _ => Task.CompletedTask;

        Assert.Throws<ArgumentNullException>(() => new CompiledPipelines(command, null!));
    }

    [Fact]
    public void Properties_ReturnSameDelegatesProvidedToConstructor()
    {
        PipelineDelegate command = _ => Task.CompletedTask;
        PipelineDelegate query = _ => Task.CompletedTask;

        var pipelines = new CompiledPipelines(command, query);

        Assert.Same(command, pipelines.Command);
        Assert.Same(query, pipelines.Query);
    }
}
