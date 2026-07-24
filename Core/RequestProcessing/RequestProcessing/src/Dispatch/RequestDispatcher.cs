using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Dispatch;
using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;
using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;
using RaccoonLand.Core.RequestProcessing.Pipeline;

namespace RaccoonLand.Core.RequestProcessing.Dispatch;

/// <summary>
/// Default dispatcher. It looks up the request's pre-registered <see cref="RequestKind"/>, builds a
/// <see cref="PipelineContext"/>, and runs it through the matching compiled pipeline.
/// </summary>
public sealed class RequestDispatcher : IRequestDispatcher
{
    private readonly CompiledPipelines _pipelines;
    private readonly EndpointInvokerRegistry _registry;

    public RequestDispatcher(CompiledPipelines pipelines, EndpointInvokerRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(pipelines);
        ArgumentNullException.ThrowIfNull(pipelines.Command);
        ArgumentNullException.ThrowIfNull(pipelines.Query);
        ArgumentNullException.ThrowIfNull(registry);
        _pipelines = pipelines;
        _registry = registry;
    }

    public async Task<PipelineResponse?> DispatchAsync(
        IRequestBase request,
        IServiceProvider requestServices,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(requestServices);

        var metadata = _registry.ResolveMetadata(request.GetType());
        var pipeline = metadata.Kind == RequestKind.Query ? _pipelines.Query : _pipelines.Command;

        var context = new PipelineContext(request, requestServices, metadata, cancellationToken);
        await pipeline(context);
        return context.Response;
    }
}
