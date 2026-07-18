using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using RaccoonLand.Modules.Observability.Logging.Serilog.Configuration;
using RaccoonLand.Modules.Observability.Logging.Serilog.Enrichers;
using Serilog;

namespace RaccoonLand.Modules.Observability.Logging.Serilog.Hosting;

/// <summary>
/// Configures Serilog for ASP.NET Core and generic .NET hosts. Reads sinks and levels from the standard
/// <c>Serilog</c> configuration section (consumer-installed sink packages) and applies RaccoonLand default
/// enrichments from <see cref="RaccoonLandSerilogOptions"/>.
/// <para>
/// <see cref="RaccoonLandSerilogOptions"/> is read once when the logger is built (a snapshot). Later
/// configuration reloads do not change these enrichments unless the host rebuilds the logger.
/// </para>
/// <para>
/// Default enrichments are registered before <c>ReadFrom.Configuration</c>. Duplicate property names follow
/// Serilog enricher semantics (typically add-if-absent for the properties this package adds); this is not a
/// hard override contract. Consumers own collision policy when they add further enrichers, sinks, or formatters.
/// </para>
/// </summary>
public static class RaccoonLandSerilogHostBuilderExtensions
{
    /// <summary>
    /// Configures Serilog from <paramref name="configuration"/> (or the host context configuration when
    /// <paramref name="configuration"/> is null). Call early in <c>Program.cs</c>, before building the app.
    /// Sinks and levels still come from the standard <c>Serilog</c> section via <c>ReadFrom.Configuration</c>.
    /// </summary>
    /// <param name="hostBuilder">The host builder.</param>
    /// <param name="configuration">Application configuration, or <see langword="null"/> to use the host context.</param>
    /// <param name="sectionName">
    /// Root configuration section for RaccoonLand enrichments (defaults to <see cref="RaccoonLandSerilogOptions.SectionName"/>).
    /// Must be non-empty. A missing or mistyped section binds to default <see cref="RaccoonLandSerilogOptions"/> values
    /// (no error).
    /// </param>
    /// <param name="configureOptions">Optional post-bind customization of <see cref="RaccoonLandSerilogOptions"/>.</param>
    public static IHostBuilder UseRaccoonLandSerilog(
        this IHostBuilder hostBuilder,
        IConfiguration? configuration = null,
        string sectionName = RaccoonLandSerilogOptions.SectionName,
        Action<RaccoonLandSerilogOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        return hostBuilder.UseSerilog((context, services, loggerConfiguration) =>
        {
            var config = configuration ?? context.Configuration;
            var options = config.GetSection(sectionName).Get<RaccoonLandSerilogOptions>()
                ?? new RaccoonLandSerilogOptions();

            configureOptions?.Invoke(options);

            ApplyDefaultEnrichments(loggerConfiguration, context, options);
            loggerConfiguration
                .ReadFrom.Configuration(config)
                .ReadFrom.Services(services);
        });
    }

    private static void ApplyDefaultEnrichments(
        LoggerConfiguration loggerConfiguration,
        HostBuilderContext context,
        RaccoonLandSerilogOptions options)
    {
        if (options.EnrichWithApplicationName)
        {
            var applicationName = string.IsNullOrWhiteSpace(options.ApplicationName)
                ? context.HostingEnvironment.ApplicationName
                : options.ApplicationName;

            loggerConfiguration.Enrich.WithProperty("ApplicationName", applicationName);
        }

        if (options.EnrichWithEnvironmentName)
        {
            loggerConfiguration.Enrich.WithProperty("EnvironmentName", context.HostingEnvironment.EnvironmentName);
        }

        if (options.EnrichWithMachineName)
        {
            loggerConfiguration.Enrich.WithProperty("MachineName", Environment.MachineName);
        }

        if (options.EnrichWithTraceId)
        {
            loggerConfiguration.Enrich.With<TraceIdEnricher>();
        }
    }
}
