namespace RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;

/// <summary>
/// A function that processes a <see cref="PipelineContext"/> — the CQRS counterpart of ASP.NET Core's
/// <c>RequestDelegate</c>.
/// </summary>
public delegate Task PipelineDelegate(PipelineContext context);

/// <summary>
/// A class-based middleware, mirroring ASP.NET Core's <c>IMiddleware</c>. Register the type as a
/// <b>singleton</b> in DI and add it to a pipeline with <c>UseMiddleware&lt;T&gt;()</c>.
/// <para>
/// Middleware is expected to be <b>stateless</b>. Request-scoped services must be resolved from
/// <see cref="PipelineContext.RequestServices"/> inside <see cref="InvokeAsync"/>, not injected via the
/// constructor.
/// </para>
/// </summary>
public interface IPipelineMiddleware
{
    /// <summary>Processes the context and, when appropriate, invokes <paramref name="next"/> to continue the pipeline.</summary>
    Task InvokeAsync(PipelineContext context, PipelineDelegate next);
}
