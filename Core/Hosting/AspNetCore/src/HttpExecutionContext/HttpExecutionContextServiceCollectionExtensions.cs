using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaccoonLand.Core.ExecutionContext.Abstractions;

namespace RaccoonLand.Core.Hosting.AspNetCore.HttpExecutionContext;

/// <summary>DI registration for HTTP-scoped <see cref="ICurrentExecutionContext"/>.</summary>
public static class HttpExecutionContextServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="HttpExecutionContext"/> as the scoped <see cref="ICurrentExecutionContext"/>
    /// implementation, bound from a configuration section. Call
    /// <see cref="HttpExecutionContextApplicationBuilderExtensions.UseRaccoonLandHttpExecutionContext"/> to
    /// populate values from each HTTP request.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="sectionName">
    /// Root configuration section (defaults to <see cref="HttpExecutionContextOptions.SectionName"/>).
    /// </param>
    /// <param name="configureOptions">Optional post-bind customization of <see cref="HttpExecutionContextOptions"/>.</param>
    public static IServiceCollection AddRaccoonLandHttpExecutionContext(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = HttpExecutionContextOptions.SectionName,
        Action<HttpExecutionContextOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<HttpExecutionContextOptions>(configuration.GetSection(sectionName));

        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }

        RegisterCore(services);
        return services;
    }

    /// <summary>
    /// Registers <see cref="HttpExecutionContext"/> configured entirely in code.
    /// </summary>
    public static IServiceCollection AddRaccoonLandHttpExecutionContext(
        this IServiceCollection services,
        Action<HttpExecutionContextOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.AddOptions<HttpExecutionContextOptions>()
            .Configure(configureOptions);

        RegisterCore(services);
        return services;
    }

    private static void RegisterCore(IServiceCollection services)
    {
        services.TryAddScoped<HttpExecutionContext>();
        services.TryAddScoped<ICurrentExecutionContext>(sp => sp.GetRequiredService<HttpExecutionContext>());
    }
}
