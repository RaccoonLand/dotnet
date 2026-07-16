using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using RaccoonLand.Core.Hosting.AspNetCore.HttpExceptionHandling;
using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;
using RaccoonLand.Modules.FileStorage.Abstractions;
using RaccoonLand.Modules.MessageLocalization.Abstraction;
using RaccoonLand.Modules.Middlewares.ExceptionHandlingMiddleware;

namespace RaccoonLand.Modules.FileStorage.AspNetCore;

/// <summary>Shared status / message mapping for FileStorage exceptions.</summary>
internal static class FileStorageExceptionMapping
{
    public static (int StatusCode, string TemplateKey) Map(FileStorageException exception)
        => exception switch
        {
            FileStorageValidationException => (StatusCodes.Status400BadRequest, FileStorageMessageTemplates.VALIDATION_FAILED),
            FileNotFoundStorageException => (StatusCodes.Status404NotFound, FileStorageMessageTemplates.FILE_NOT_FOUND),
            FileAlreadyExistsStorageException => (StatusCodes.Status409Conflict, FileStorageMessageTemplates.FILE_ALREADY_EXISTS),
            FileAccessDeniedStorageException => (StatusCodes.Status403Forbidden, FileStorageMessageTemplates.ACCESS_DENIED),
            FileStorageUnavailableException => (StatusCodes.Status503ServiceUnavailable, FileStorageMessageTemplates.UNAVAILABLE),
            FileStorageNotSupportedException => (StatusCodes.Status501NotImplemented, FileStorageMessageTemplates.NOT_SUPPORTED),
            FileStorageConfigurationException => (StatusCodes.Status500InternalServerError, FileStorageMessageTemplates.CONFIGURATION_ERROR),
            _ => (StatusCodes.Status500InternalServerError, FileStorageMessageTemplates.OPERATION_FAILED),
        };

    public static PipelineResponse ToPipelineResponse(
        FileStorageException exception,
        IServiceProvider? services)
    {
        var (status, templateKey) = Map(exception);
        var message = ResolveMessage(templateKey, services);

        return new PipelineResponse
        {
            StatusHint = status,
            Errors = [new PipelineMessage(templateKey, message)],
        };
    }

    private static string ResolveMessage(
        string templateKey,
        IServiceProvider? services)
    {
        // Never use exception.Message in the API body — it may include storage keys.
        var localizer = services?.GetService<IMessageLocalization>();
        return localizer is null ? templateKey : localizer[templateKey];
    }
}

/// <summary>Registers FileStorage exception handlers on the HTTP and request-pipeline exception options.</summary>
public static class FileStorageExceptionHandlingExtensions
{
    /// <summary>
    /// Registers handlers for <see cref="FileStorageException"/> types on
    /// <see cref="HttpExceptionHandlingOptions"/> and <see cref="ExceptionHandlingOptions"/>.
    /// Call alongside <c>AddRaccoonLandHttpExceptionHandling</c> / <c>AddRaccoonLandExceptionHandling</c>.
    /// Uses <c>PostConfigure</c> so existing handlers remain.
    /// Messages are resolved via <see cref="IMessageLocalization"/> when registered.
    /// </summary>
    public static IServiceCollection AddRaccoonLandFileStorageExceptionHandling(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.PostConfigure<HttpExceptionHandlingOptions>(RegisterHttpHandlers);
        services.PostConfigure<ExceptionHandlingOptions>(RegisterPipelineHandlers);

        return services;
    }

    /// <summary>Adds FileStorage handlers to an existing <see cref="HttpExceptionHandlingOptions"/> instance.</summary>
    public static HttpExceptionHandlingOptions AddFileStorageHandlers(this HttpExceptionHandlingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        RegisterHttpHandlers(options);
        return options;
    }

    /// <summary>Adds FileStorage handlers to an existing <see cref="ExceptionHandlingOptions"/> instance.</summary>
    public static ExceptionHandlingOptions AddFileStorageHandlers(this ExceptionHandlingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        RegisterPipelineHandlers(options);
        return options;
    }

    private static void RegisterHttpHandlers(HttpExceptionHandlingOptions options)
    {
        options.On<FileStorageException>(async (httpContext, exception) =>
        {
            var envelope = FileStorageExceptionMapping.ToPipelineResponse(
                exception,
                httpContext.RequestServices);
            httpContext.Response.StatusCode = envelope.StatusHint
                ?? StatusCodes.Status500InternalServerError;
            await httpContext.Response.WriteAsJsonAsync(envelope);
            return true;
        });
    }

    private static void RegisterPipelineHandlers(ExceptionHandlingOptions options)
    {
        options.On<FileStorageException>((context, exception) =>
        {
            context.Response = FileStorageExceptionMapping.ToPipelineResponse(
                exception,
                context.RequestServices);
            return Task.FromResult(true);
        });
    }
}
