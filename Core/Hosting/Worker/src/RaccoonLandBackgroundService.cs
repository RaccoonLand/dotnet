using Microsoft.Extensions.Hosting;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;

namespace RaccoonLand.Core.Hosting.Worker;

/// <summary>
/// Base class for generic RaccoonLand background workers. Provides <see cref="DispatchAsync"/> helpers that
/// forward a request to <see cref="WorkerRequestDispatcher"/> inside a per-job DI scope.
/// </summary>
/// <remarks>
/// Derive from this type, implement <see cref="BackgroundService.ExecuteAsync"/>, and call
/// <c>DispatchAsync</c> for each unit of work. Business logic belongs in
/// <see cref="IEndpoint{TRequest}"/> handlers, not in the worker.
/// </remarks>
public abstract class RaccoonLandBackgroundService(WorkerRequestDispatcher workerDispatcher) : BackgroundService
{
    private readonly WorkerRequestDispatcher _workerDispatcher = workerDispatcher;

    /// <summary>Dispatches a void request through the pipeline inside a per-job scope.</summary>
    protected Task<WorkerDispatchResult> DispatchAsync(
        IRequest request,
        WorkerExecutionMetadata? metadata = null,
        CancellationToken cancellationToken = default)
        => _workerDispatcher.DispatchAsync(request, metadata, cancellationToken);

    /// <summary>Dispatches a request that produces a <typeparamref name="TResponse"/> through the pipeline.</summary>
    protected Task<WorkerDispatchResult> DispatchAsync<TResponse>(
        IRequest<TResponse> request,
        WorkerExecutionMetadata? metadata = null,
        CancellationToken cancellationToken = default)
        => _workerDispatcher.DispatchAsync(request, metadata, cancellationToken);
}
