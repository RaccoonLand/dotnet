using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;

namespace RaccoonLand.Modules.Middlewares.ExceptionHandlingMiddleware;

/// <summary>
/// Registration for the exception-handling pipeline middleware. Registers the middleware as a singleton
/// (stateless; request-scoped services via <see cref="PipelineContext.RequestServices"/>). Call this, then
/// add the middleware to a pipeline with <c>pipeline.UseMiddleware&lt;ExceptionHandlingMiddleware&gt;()</c>.
/// </summary>
public static class ExceptionHandlingServiceCollectionExtensions
{
    public static IServiceCollection AddRaccoonLandExceptionHandling(
        this IServiceCollection services,
        Action<ExceptionHandlingOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<ExceptionHandlingOptions>();
        services.TryAddSingleton<ExceptionHandlingMiddleware>();

        if (configure is not null)
        {
            services.Configure(configure);
        }

        return services;
    }
}
