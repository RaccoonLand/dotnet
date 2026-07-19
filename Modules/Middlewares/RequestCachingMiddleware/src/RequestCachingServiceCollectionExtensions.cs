using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace RaccoonLand.Modules.Middlewares.RequestCachingMiddleware;

/// <summary>
/// Registration for the request-caching pipeline middleware. Registers the middleware as a singleton
/// (stateless; <c>IDistributedCache</c> and <see cref="IOptionsMonitor{TOptions}"/> are used per invocation).
/// Call this, then add the middleware to a pipeline with
/// <c>pipeline.UseMiddleware&lt;RequestCachingMiddleware&gt;()</c>. The consumer must also register an
/// <c>IDistributedCache</c> (for example <c>AddDistributedMemoryCache</c>).
/// </summary>
public static class RequestCachingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the middleware and binds <see cref="RequestCachingOptions"/> from a configuration section.
    /// Nested keys such as <c>Default</c> and <c>Overrides</c> are fixed relative to the root section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="sectionName">Root configuration section name (defaults to <see cref="RequestCachingOptions.SectionName"/>).</param>
    /// <param name="configure">Optional post-bind customization of <see cref="RequestCachingOptions"/>.</param>
    public static IServiceCollection AddRaccoonLandRequestCaching(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = RequestCachingOptions.SectionName,
        Action<RequestCachingOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        var builder = services.AddOptions<RequestCachingOptions>()
            .Bind(configuration.GetSection(sectionName));

        if (configure is not null)
        {
            builder.Configure(configure);
        }

        builder.Validate(ValidateDurations, DurationValidationMessage)
            .ValidateOnStart();

        services.TryAddSingleton<RequestCachingMiddleware>();

        return services;
    }

    /// <summary>
    /// Registers the middleware and configures <see cref="RequestCachingOptions"/> in code only.
    /// </summary>
    public static IServiceCollection AddRaccoonLandRequestCaching(
        this IServiceCollection services,
        Action<RequestCachingOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var builder = services.AddOptions<RequestCachingOptions>();

        if (configure is not null)
        {
            builder.Configure(configure);
        }

        builder.Validate(ValidateDurations, DurationValidationMessage)
            .ValidateOnStart();

        services.TryAddSingleton<RequestCachingMiddleware>();

        return services;
    }

    private const string DurationValidationMessage =
        "RequestCaching Duration values must be greater than TimeSpan.Zero " +
        "(Default and every Overrides entry).";

    private static bool ValidateDurations(RequestCachingOptions options)
    {
        if (options.Default is null || options.Default.Duration <= TimeSpan.Zero)
        {
            return false;
        }

        if (options.Overrides is null)
        {
            return false;
        }

        foreach (var entry in options.Overrides.Values)
        {
            if (entry is null || entry.Duration <= TimeSpan.Zero)
            {
                return false;
            }
        }

        return true;
    }
}
