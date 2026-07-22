using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;
using RaccoonLand.Core.RequestProcessing.Pipeline;

namespace RaccoonLand.Core.RequestProcessing.Tests.Support;

internal sealed class TestPipelineBuilder : PipelineBuilder
{
    public TestPipelineBuilder(IServiceProvider applicationServices, PipelineDelegate terminal)
        : base(applicationServices, terminal)
    {
    }
}
