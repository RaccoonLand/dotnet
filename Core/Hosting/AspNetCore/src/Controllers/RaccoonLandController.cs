using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using RaccoonLand.Core.Hosting.AspNetCore.PipelineResponseMapping;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Dispatch;

namespace RaccoonLand.Core.Hosting.AspNetCore.Controllers;

/// <summary>
/// Base controller for RaccoonLand feature controllers. Provides <see cref="DispatchAsync"/> helpers that
/// forward a request to <see cref="IRequestDispatcher"/> using the current HTTP request scope and map the
/// resulting <see cref="RaccoonLand.Core.RequestProcessing.Abstractions.Responses.PipelineResponse"/> to an
/// <see cref="IActionResult"/>.
/// </summary>
/// <remarks>
/// Controllers should stay thin: bind the request (route/query/body), call <c>DispatchAsync</c>, return the
/// result. Business logic belongs in <see cref="IEndpoint{TRequest}"/> handlers, not in the controller.
/// <see cref="IRequestDispatcher"/> and <see cref="IPipelineResponseMapper"/> are resolved from
/// <c>HttpContext.RequestServices</c> on each dispatch.
/// </remarks>
public abstract class RaccoonLandController : ControllerBase
{
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
        var dispatcher = HttpContext.RequestServices.GetRequiredService<IRequestDispatcher>();
        var responseMapper = HttpContext.RequestServices.GetRequiredService<IPipelineResponseMapper>();

        var response = await dispatcher.DispatchAsync(
            request,
            HttpContext.RequestServices,
            cancellationToken);

        return responseMapper.Map(response);
    }
}
