using Microsoft.Extensions.DependencyInjection;

namespace RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;

/// <summary>
/// Convenience overloads for registering middleware, mirroring ASP.NET Core's <c>UseExtensions</c> and
/// <c>UseMiddlewareExtensions</c>. Custom-middleware packages build their own <c>UseXxx()</c> helpers on
/// top of these.
/// </summary>
public static class PipelineBuilderExtensions
{
    /// <summary>
    /// Adds an inline middleware. Mirrors <c>app.Use(async (context, next) =&gt; { ...; await next(); })</c>.
    /// </summary>
    public static IPipelineBuilder Use(
        this IPipelineBuilder builder,
        Func<PipelineContext, Func<Task>, Task> middleware)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(middleware);

        return builder.Use(next => context => middleware(context, () => next(context)));
    }

    /// <summary>
    /// Adds a class-based middleware resolved once from <see cref="IPipelineBuilder.ApplicationServices"/>
    /// when <c>UseMiddleware&lt;T&gt;</c> is called (the instance is then captured in the compiled pipeline).
    /// Middleware must be registered as a <b>singleton</b> and stay stateless. Request-scoped services must be
    /// resolved from <see cref="PipelineContext.RequestServices"/> inside
    /// <see cref="IPipelineMiddleware.InvokeAsync"/> — never via the middleware constructor.
    /// </summary>
    public static IPipelineBuilder UseMiddleware<TMiddleware>(this IPipelineBuilder builder)
        where TMiddleware : IPipelineMiddleware
    {
        ArgumentNullException.ThrowIfNull(builder);

        var middleware = builder.ApplicationServices.GetRequiredService<TMiddleware>();

        return builder.Use(next => context => middleware.InvokeAsync(context, next));
    }
}
