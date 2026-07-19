using Microsoft.Extensions.DependencyInjection;
using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;
using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;
using RaccoonLand.Modules.MessageLocalization.Abstraction;
using RaccoonLand.Modules.Security.Authorization.Abstractions;

namespace RaccoonLand.Modules.Security.Authorization.Middlewares;

/// <summary>
/// Pipeline middleware that authorizes the current request before it reaches the terminal endpoint. It
/// resolves the active <see cref="IAuthorizationProvider"/> from <see cref="PipelineContext.RequestServices"/>
/// and asks it to authorize the request, keyed by the concrete request type's <c>FullName</c>.
/// <para>
/// The dispatcher is expected to place the concrete request instance in <see cref="PipelineContext.Request"/>.
/// Proxies, decorators, or subclasses that change the runtime type are not supported as authorization keys —
/// a mismatch yields deny-by-default (fail-closed), not a security bypass.
/// </para>
/// <para>
/// When the decision is <see cref="AuthorizationStatus.Allowed"/> the pipeline continues. Otherwise the
/// pipeline is short-circuited with a <see cref="PipelineResponse"/> error envelope. The middleware — not the
/// provider — owns the response: it picks the stable code and localization template
/// (see <see cref="AuthorizationMessageTemplates"/>), resolves the text through <see cref="IMessageLocalization"/>
/// when one is registered, and sets <c>StatusHint</c> (401 for unauthenticated, 403 for denied).
/// </para>
/// <para>
/// Infrastructure failures from the provider (timeouts, transport errors, database failures) propagate as
/// exceptions and are <b>not</b> converted to <see cref="AuthorizationStatus.Denied"/>. Host exception handling
/// must manage those failures. An <see cref="IAuthorizationProvider"/> must already be registered; otherwise
/// the first authorized request fails with a DI resolution exception (not a startup check).
/// </para>
/// </summary>
public sealed class AuthorizationMiddleware : IPipelineMiddleware
{
    public async Task InvokeAsync(PipelineContext context, PipelineDelegate next)
    {
        var requestType = context.Request.GetType();
        var requestName = requestType.FullName
            ?? throw new InvalidOperationException(
                $"Authorization requires a request type with a non-null {nameof(Type.FullName)}. " +
                $"Type '{requestType.Name}' cannot be used as an authorization key. " +
                "Ensure the pipeline dispatches the concrete request type.");

        var provider = context.RequestServices.GetRequiredService<IAuthorizationProvider>();
        var decision = await provider.AuthorizeAsync(
            new AuthorizationContext(requestName),
            context.CancellationToken);

        if (decision.IsAllowed)
        {
            await next(context);
            return;
        }

        context.Response = BuildResponse(context, decision.Status);
    }

    private static PipelineResponse BuildResponse(PipelineContext context, AuthorizationStatus status)
    {
        var (template, statusHint) = status == AuthorizationStatus.Unauthenticated
            ? (AuthorizationMessageTemplates.AuthenticationRequired, 401)
            : (AuthorizationMessageTemplates.AccessDenied, 403);

        var localizer = context.RequestServices.GetService<IMessageLocalization>();
        var message = localizer is null ? template : localizer.Get(template);

        return new PipelineResponse
        {
            StatusHint = statusHint,
            Errors = [new PipelineMessage(template, message)],
        };
    }
}
