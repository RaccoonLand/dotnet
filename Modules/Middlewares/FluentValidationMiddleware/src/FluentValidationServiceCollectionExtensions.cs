using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;

namespace RaccoonLand.Modules.Middlewares.FluentValidationMiddleware;

/// <summary>
/// Registration for the FluentValidation pipeline middleware. Registers the middleware as a singleton
/// (stateless; validators resolved from <see cref="PipelineContext.RequestServices"/>). Call this, then add
/// the middleware to a pipeline with <c>pipeline.UseMiddleware&lt;FluentValidationMiddleware&gt;()</c>.
/// Register validators with FluentValidation's own helpers (for example
/// <c>services.AddValidatorsFromAssembly(...)</c>).
/// </summary>
public static class FluentValidationServiceCollectionExtensions
{
    public static IServiceCollection AddRaccoonLandFluentValidation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<FluentValidationMiddleware>();

        return services;
    }
}
