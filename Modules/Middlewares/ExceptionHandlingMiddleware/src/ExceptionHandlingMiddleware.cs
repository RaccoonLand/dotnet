using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RaccoonLand.Core.Domain.Exceptions;
using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;
using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;
using RaccoonLand.Modules.MessageLocalization.Abstraction;

namespace RaccoonLand.Modules.Middlewares.ExceptionHandlingMiddleware;

/// <summary>
/// Pipeline middleware that turns request-level exceptions into a <see cref="PipelineResponse"/> error envelope.
/// Resolution order: developer-registered handlers, then <see cref="DomainException"/> (localized message when
/// <see cref="IMessageLocalization"/> is available). Any other exception is rethrown.
/// </summary>
public sealed class ExceptionHandlingMiddleware(IOptions<ExceptionHandlingOptions> options) : IPipelineMiddleware
{
    private readonly ExceptionHandlingOptions _options = options.Value;

    public async Task InvokeAsync(PipelineContext context, PipelineDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            if (await TryHandleWithCustomAsync(context, exception))
            {
                return;
            }

            if (exception is DomainException domainException)
            {
                HandleDomainException(context, domainException);
                return;
            }

            throw;
        }
    }

    private async Task<bool> TryHandleWithCustomAsync(PipelineContext context, Exception exception)
    {
        foreach (var handler in _options.Handlers)
        {
            if (handler.ExceptionType.IsInstanceOfType(exception)
                && await handler.Handler(context, exception))
            {
                return true;
            }
        }

        return false;
    }

    private static void HandleDomainException(PipelineContext context, DomainException exception)
    {
        var localizer = context.RequestServices.GetService<IMessageLocalization>();

        var messages = exception.Errors
            .Select(error => ToPipelineMessage(error, localizer))
            .ToArray();

        context.Response = new PipelineResponse
        {
            Errors = messages,
        };
    }

    private static PipelineMessage ToPipelineMessage(DomainError error, IMessageLocalization? localizer)
    {
        if (localizer is null)
        {
            return new PipelineMessage(error.Code, error.Message);
        }

        // Spread into params object?[] for IMessageLocalization.
        return new PipelineMessage(error.Code, localizer.Get(error.Message, error.Parameters.ToArray()));
    }
}
