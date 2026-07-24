using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using RaccoonLand.Core.Hosting.AspNetCore.PipelineResponseMapping;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Dispatch;
using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;

namespace RaccoonLand.Core.Hosting.AspNetCore.Controllers;

/// <summary>
/// Base controller for RaccoonLand feature controllers. Provides <see cref="DispatchAsync"/> helpers that
/// forward a request to <see cref="IRequestDispatcher"/> using the current HTTP request scope and map the
/// resulting <see cref="PipelineResponse"/> to an <see cref="IActionResult"/>.
/// </summary>
/// <remarks>
/// Controllers should stay thin: bind the request (route/query/body), call <c>DispatchAsync</c>, return the
/// result. Business logic belongs in <see cref="IEndpoint{TRequest}"/> handlers, not in the controller.
/// <see cref="IRequestDispatcher"/> and <see cref="IPipelineResponseMapper"/> are resolved from
/// <c>HttpContext.RequestServices</c> on each dispatch — no constructor injection is needed. For advanced
/// scenarios where the default envelope-to-HTTP mapping does not fit (for example returning
/// <see cref="ControllerBase.File(byte[], string, string)"/> when the pipeline result is a file), use the
/// <see cref="Dispatcher"/> and <see cref="ResponseMapper"/> properties directly.
/// </remarks>
public abstract class RaccoonLandController : ControllerBase
{
    /// <summary>
    /// The request dispatcher for the current HTTP request. Resolved from
    /// <c>HttpContext.RequestServices</c> on each access.
    /// <para>
    /// Prefer <see cref="DispatchAsync(IRequest, CancellationToken)"/> and
    /// <see cref="DispatchAsync{TResponse}(IRequest{TResponse}, CancellationToken)"/> for the standard
    /// "dispatch + envelope-to-HTTP" flow — those helpers also apply the
    /// <c>HttpContext.RequestAborted</c> cancellation fallback. Use this property only for advanced
    /// scenarios where the default mapping does not fit — for example a file download that needs to
    /// inspect <see cref="PipelineResponse.Result"/> before choosing between
    /// <see cref="ControllerBase.File(byte[], string, string)"/>, <see cref="ControllerBase.NotFound()"/>,
    /// and falling back to <see cref="ResponseMapper"/> for errors and <c>StatusHint</c>.
    /// </para>
    /// </summary>
    protected IRequestDispatcher Dispatcher
        => HttpContext.RequestServices.GetRequiredService<IRequestDispatcher>();

    /// <summary>
    /// The mapper from <see cref="PipelineResponse"/> to <see cref="IActionResult"/> for the current HTTP
    /// request. Resolved from <c>HttpContext.RequestServices</c> on each access. Pair with
    /// <see cref="Dispatcher"/> when producing a custom success result but delegating the failure /
    /// warnings / <c>StatusHint</c> path to the standard policy.
    /// </summary>
    protected IPipelineResponseMapper ResponseMapper
        => HttpContext.RequestServices.GetRequiredService<IPipelineResponseMapper>();

    /// <summary>
    /// Dispatches a void request (command with no response) through the pipeline and maps the envelope to HTTP.
    /// </summary>
    protected Task<IActionResult> DispatchAsync(
        IRequest request,
        CancellationToken cancellationToken = default)
        => DispatchCoreAsync(request, cancellationToken);

    /// <summary>
    /// Dispatches a request that produces a <typeparamref name="TResponse"/> through the pipeline and maps the
    /// envelope to HTTP.
    /// </summary>
    protected Task<IActionResult> DispatchAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
        => DispatchCoreAsync(request, cancellationToken);

    private async Task<IActionResult> DispatchCoreAsync(
        IRequestBase request,
        CancellationToken cancellationToken)
    {
        // If the caller forgot to accept + forward a CancellationToken action parameter, fall back
        // to HttpContext.RequestAborted so a client disconnect still cancels the pipeline (DB
        // connections, retries, caches, ...). When the caller *did* pass a real token, respect it
        // as-is — they own the semantics.
        var effectiveToken = cancellationToken.CanBeCanceled
            ? cancellationToken
            : HttpContext.RequestAborted;

        var response = await Dispatcher.DispatchAsync(
            request,
            HttpContext.RequestServices,
            effectiveToken);

        return ResponseMapper.Map(response);
    }
}
