using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;

namespace RaccoonLand.Modules.Middlewares.ExceptionHandlingMiddleware;

/// <summary>
/// Options for <see cref="ExceptionHandlingMiddleware"/>. Register handlers for specific exception types with
/// <see cref="On{TException}"/>; they take precedence over the built-in
/// <see cref="RaccoonLand.Core.Domain.Exceptions.DomainException"/> handling.
/// </summary>
public sealed class ExceptionHandlingOptions
{
    private readonly List<ExceptionHandlerRegistration> _handlers = [];

    /// <summary>The registered custom handlers, in registration order.</summary>
    public IReadOnlyList<ExceptionHandlerRegistration> Handlers => _handlers;

    /// <summary>
    /// Registers a handler for <typeparamref name="TException"/> (and derived types). The handler shapes
    /// <see cref="PipelineContext.Response"/> and returns <c>true</c> when it has handled the exception;
    /// returning <c>false</c> lets the next handler (or the built-in handling) run.
    /// </summary>
    public ExceptionHandlingOptions On<TException>(Func<PipelineContext, TException, Task<bool>> handler)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(handler);

        _handlers.Add(new ExceptionHandlerRegistration(
            typeof(TException),
            (context, exception) => handler(context, (TException)exception)));

        return this;
    }

    /// <summary>A custom exception handler bound to a specific exception type.</summary>
    public sealed record ExceptionHandlerRegistration(
        Type ExceptionType,
        Func<PipelineContext, Exception, Task<bool>> Handler);
}
