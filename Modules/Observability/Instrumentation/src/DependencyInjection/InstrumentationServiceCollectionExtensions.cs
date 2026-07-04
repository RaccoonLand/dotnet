using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaccoonLand.Modules.Observability.Instrumentation.Configuration;
using RaccoonLand.Modules.Observability.Instrumentation.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registration for the pipeline instrumentation middleware. Registers
/// <see cref="PipelineInstrumentationMiddleware"/> as a singleton (stateless; scoped services are resolved per
/// request from <c>PipelineContext.RequestServices</c>). After calling this, add it as the outermost middleware
/// in each pipeline with <c>pipeline.UseMiddleware&lt;PipelineInstrumentationMiddleware&gt;()</c>.
/// </summary>
public static class InstrumentationServiceCollectionExtensions
{
    /// <summary>Registers the instrumentation middleware and configures its toggles in code.</summary>
    public static IServiceCollection AddRaccoonLandPipelineInstrumentation(
        this IServiceCollection services,
        Action<InstrumentationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<InstrumentationOptions>();
        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.TryAddSingleton<PipelineInstrumentationMiddleware>();

        return services;
    }

    /// <summary>
    /// Registers the instrumentation middleware and binds its toggles from the given configuration
    /// <paramref name="sectionName"/> (defaults to <c>Observability:Instrumentation</c>).
    /// </summary>
    public static IServiceCollection AddRaccoonLandPipelineInstrumentation(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = InstrumentationOptions.SectionName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<InstrumentationOptions>()
            .Bind(configuration.GetSection(sectionName));

        services.TryAddSingleton<PipelineInstrumentationMiddleware>();

        return services;
    }
}
