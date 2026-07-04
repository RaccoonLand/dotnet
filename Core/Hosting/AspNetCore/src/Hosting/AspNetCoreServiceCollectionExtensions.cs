using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaccoonLand.Core.Hosting.AspNetCore.HttpExecutionContext;
using RaccoonLand.Core.Hosting.AspNetCore.PipelineResponseMapping;

namespace RaccoonLand.Core.Hosting.AspNetCore.Hosting;

/// <summary>Registers ASP.NET Core hosting services for RaccoonLand.</summary>
public static class AspNetCoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers the default <see cref="IPipelineResponseMapper"/> when none is already registered.
    /// When <paramref name="configuration"/> is supplied, also registers HTTP
    /// <see cref="RaccoonLand.Core.ExecutionContext.Abstractions.ICurrentExecutionContext"/> from
    /// <see cref="HttpExecutionContextOptions"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">
    /// When provided, binds <see cref="HttpExecutionContextOptions"/> and registers
    /// <c>AddRaccoonLandHttpExecutionContext</c>.
    /// </param>
    /// <param name="configureExecutionContext">Optional post-bind customization of execution-context mapping.</param>
    public static IServiceCollection AddRaccoonLandAspNetCore(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        Action<HttpExecutionContextOptions>? configureExecutionContext = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IPipelineResponseMapper, DefaultPipelineResponseMapper>();

        if (configuration is not null)
        {
            services.AddRaccoonLandHttpExecutionContext(configuration, configureOptions: configureExecutionContext);
        }
        else if (configureExecutionContext is not null)
        {
            services.AddRaccoonLandHttpExecutionContext(configureExecutionContext);
        }

        return services;
    }
}
