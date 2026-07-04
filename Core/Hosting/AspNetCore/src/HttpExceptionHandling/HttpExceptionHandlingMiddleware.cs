using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RaccoonLand.Core.Hosting.AspNetCore.HttpExceptionHandling;

/// <summary>
/// ASP.NET Core HTTP middleware that turns unhandled request exceptions into HTTP responses. Resolution order:
/// developer-registered handlers, then any other unhandled exception (HTTP 500
/// <see cref="ProblemDetails"/>, with details logged but not leaked).
/// <para>
/// Not to be confused with pipeline
/// <c>RaccoonLand.Modules.Middlewares.ExceptionHandlingMiddleware.ExceptionHandlingMiddleware</c>, which is a
/// host-agnostic <c>IPipelineMiddleware</c> for the RaccoonLand request pipeline.
/// </para>
/// </summary>
public sealed class HttpExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HttpExceptionHandlingOptions _options;
    private readonly ILogger<HttpExceptionHandlingMiddleware> _logger;

    public HttpExceptionHandlingMiddleware(
        RequestDelegate next,
        IOptions<HttpExceptionHandlingOptions> options,
        ILogger<HttpExceptionHandlingMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _next = next;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception exception)
        {
            if (httpContext.Response.HasStarted)
            {
                _logger.LogError(
                    exception,
                    "An exception was thrown after the response started for {Method} {Path}.",
                    httpContext.Request.Method,
                    httpContext.Request.Path);

                throw;
            }

            if (await TryHandleWithCustomAsync(httpContext, exception))
            {
                return;
            }

            await HandleUnexpectedAsync(httpContext, exception);
        }
    }

    private async Task<bool> TryHandleWithCustomAsync(HttpContext httpContext, Exception exception)
    {
        foreach (var handler in _options.Handlers)
        {
            if (handler.ExceptionType.IsInstanceOfType(exception)
                && await handler.Handler(httpContext, exception))
            {
                return true;
            }
        }

        return false;
    }

    private async Task HandleUnexpectedAsync(HttpContext httpContext, Exception exception)
    {
        _logger.LogError(
            exception,
            "Unhandled exception while processing {Method} {Path}.",
            httpContext.Request.Method,
            httpContext.Request.Path);

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
        };

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problem);
    }
}
