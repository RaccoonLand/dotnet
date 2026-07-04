using RaccoonLand.Core.RequestProcessing.Dispatch;

namespace RaccoonLand.Core.RequestProcessing.Pipeline;

/// <summary>
/// The builder for the query pipeline. Its terminal delegate resolves and invokes the endpoint
/// registered for the request type.
/// </summary>
public sealed class QueryPipelineBuilder : PipelineBuilder
{
    public QueryPipelineBuilder(IServiceProvider applicationServices, EndpointInvokerRegistry registry)
        : base(applicationServices, context => registry.Resolve(context.Request.GetType())(context))
    {
    }
}
