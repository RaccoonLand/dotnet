using Microsoft.AspNetCore.Http;

namespace RaccoonLand.Core.Hosting.AspNetCore.HttpExceptionHandling;

/// <summary>
/// Options for <see cref="HttpExceptionHandlingMiddleware"/>. Register handlers for specific exception types with
/// <see cref="On{TException}"/>; they take precedence over the built-in unexpected-exception handling.
/// </summary>
public sealed class HttpExceptionHandlingOptions
{
    private readonly List<ExceptionHandlerRegistration> _handlers = [];

    /// <summary>The registered custom handlers, in registration order.</summary>
    public IReadOnlyList<ExceptionHandlerRegistration> Handlers => _handlers;

    /// <summary>
    /// Registers a handler for <typeparamref name="TException"/> (and derived types). The handler writes the
    /// HTTP response and returns <c>true</c> when it has handled the exception; returning <c>false</c> lets the
    /// next handler (or the built-in handling) run.
    /// </summary>
    public HttpExceptionHandlingOptions On<TException>(Func<HttpContext, TException, Task<bool>> handler)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(handler);

        _handlers.Add(new ExceptionHandlerRegistration(
            typeof(TException),
            (httpContext, exception) => handler(httpContext, (TException)exception)));

        return this;
    }

    /// <summary>A custom exception handler bound to a specific exception type.</summary>
    public sealed record ExceptionHandlerRegistration(
        Type ExceptionType,
        Func<HttpContext, Exception, Task<bool>> Handler);
}
