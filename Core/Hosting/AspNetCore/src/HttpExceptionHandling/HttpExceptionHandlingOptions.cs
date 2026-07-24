using Microsoft.AspNetCore.Http;

namespace RaccoonLand.Core.Hosting.AspNetCore.HttpExceptionHandling;

/// <summary>
/// Options for <see cref="HttpExceptionHandlingMiddleware"/>. Register handlers for specific exception types with
/// <see cref="On{TException}"/>; they take precedence over the built-in unexpected-exception handling.
/// </summary>
public sealed class HttpExceptionHandlingOptions
{
    private readonly List<ExceptionHandlerRegistration> _handlers = [];

    /// <summary>
    /// The registered custom handlers, in registration order. Exposed as a read-only view — the
    /// returned instance cannot be downcast to a mutable collection to bypass <see cref="On{TException}"/>.
    /// </summary>
    public IReadOnlyList<ExceptionHandlerRegistration> Handlers => _handlers.AsReadOnly();

    /// <summary>
    /// Registers a handler for <typeparamref name="TException"/> (and derived types). Handlers are tried in
    /// <b>registration order</b> with <see cref="Type.IsInstanceOfType"/> — register more specific types
    /// <b>before</b> broader ones (for example <c>On&lt;DbUpdateException&gt;</c> before <c>On&lt;Exception&gt;</c>),
    /// otherwise an early broad handler can shadow later specific ones.
    /// <para>
    /// The handler must write a complete HTTP response (at least a status code, and a body when appropriate)
    /// and return <c>true</c> only then. Return <c>false</c> if you did not handle the exception so the next
    /// handler or the built-in fallback can run.
    /// </para>
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
