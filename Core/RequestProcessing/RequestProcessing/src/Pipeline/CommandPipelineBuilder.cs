using RaccoonLand.Core.RequestProcessing.Dispatch;

namespace RaccoonLand.Core.RequestProcessing.Pipeline;

/// <summary>
/// The builder for the command pipeline. Its terminal delegate resolves and invokes the endpoint
/// registered for the request type.
/// </summary>
public sealed class CommandPipelineBuilder : PipelineBuilder
{
    public CommandPipelineBuilder(IServiceProvider applicationServices, EndpointInvokerRegistry registry)
        : base(applicationServices, context => registry.Resolve(context.Request.GetType())(context))
    {
    }
}
