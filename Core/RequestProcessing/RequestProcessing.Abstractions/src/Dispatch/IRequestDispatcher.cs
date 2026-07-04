using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;

namespace RaccoonLand.Core.RequestProcessing.Abstractions.Dispatch;

/// <summary>
/// Sends a request through the appropriate pipeline (command or query) and returns its response.
/// <para>
/// Host-agnostic: the caller supplies the per-request <see cref="IServiceProvider"/> (the request scope) and
/// a <see cref="CancellationToken"/>. A host adapter (an API endpoint, a worker, a test) is responsible for
/// opening the scope and forwarding the token.
/// </para>
/// </summary>
public interface IRequestDispatcher
{
    /// <summary>
    /// Dispatches <paramref name="request"/> through the matching pipeline using
    /// <paramref name="requestServices"/> as the per-request scope and returns the resulting envelope.
    /// </summary>
    Task<PipelineResponse?> DispatchAsync(
        IRequestBase request,
        IServiceProvider requestServices,
        CancellationToken cancellationToken = default);
}
