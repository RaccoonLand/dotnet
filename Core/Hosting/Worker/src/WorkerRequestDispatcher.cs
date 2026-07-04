using Microsoft.Extensions.DependencyInjection;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Dispatch;

namespace RaccoonLand.Core.Hosting.Worker;

/// <summary>
/// Dispatches a request through <see cref="IRequestDispatcher"/> inside a fresh DI scope per call, configures
/// <see cref="WorkerExecutionContext"/> from optional metadata, and interprets the
/// <see cref="RaccoonLand.Core.RequestProcessing.Abstractions.Responses.PipelineResponse"/> via <see cref="IPipelineResponseHandler"/>.
/// </summary>
public sealed class WorkerRequestDispatcher(
    IRequestDispatcher dispatcher,
    IServiceScopeFactory scopeFactory,
    IPipelineResponseHandler responseHandler)
{
    private readonly IRequestDispatcher _dispatcher = dispatcher;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IPipelineResponseHandler _responseHandler = responseHandler;

    public async Task<WorkerDispatchResult> DispatchAsync(
        IRequestBase request,
        WorkerExecutionMetadata? metadata = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var scope = _scopeFactory.CreateAsyncScope();
        ConfigureExecutionContext(scope.ServiceProvider, metadata);

        var response = await _dispatcher.DispatchAsync(
            request,
            scope.ServiceProvider,
            cancellationToken);

        return _responseHandler.Handle(response);
    }

    private static void ConfigureExecutionContext(IServiceProvider serviceProvider, WorkerExecutionMetadata? metadata)
    {
        var executionContext = serviceProvider.GetService<WorkerExecutionContext>();
        executionContext?.Configure(metadata);
    }
}
