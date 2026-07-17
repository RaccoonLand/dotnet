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
}

/// <summary>A custom exception handler bound to a specific exception type.</summary>
/// <remarks>
/// Prefer <see cref="ExceptionHandlingOptions.On{TException}"/>. Constructing this type directly is supported
/// but requires a non-null <see cref="Exception"/>-derived type and handler.
/// </remarks>
public sealed record ExceptionHandlerRegistration
{
    public ExceptionHandlerRegistration(
        Type exceptionType,
        Func<PipelineContext, Exception, Task<bool>> handler)
    {
        ArgumentNullException.ThrowIfNull(exceptionType);
        ArgumentNullException.ThrowIfNull(handler);

        if (!typeof(Exception).IsAssignableFrom(exceptionType))
        {
            throw new ArgumentException(
                $"Type '{exceptionType}' must derive from {nameof(Exception)}.",
                nameof(exceptionType));
        }

        ExceptionType = exceptionType;
        Handler = handler;
    }

    /// <summary>Exception type (and derived types) this handler applies to.</summary>
    public Type ExceptionType { get; }

    /// <summary>Handler invoked when the caught exception matches <see cref="ExceptionType"/>.</summary>
    public Func<PipelineContext, Exception, Task<bool>> Handler { get; }
}
