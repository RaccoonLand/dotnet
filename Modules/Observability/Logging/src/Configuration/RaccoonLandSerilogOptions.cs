namespace RaccoonLand.Modules.Observability.Logging.Serilog.Configuration;

/// <summary>
/// RaccoonLand-specific Serilog settings, bound from configuration alongside the standard <c>Serilog</c> section.
/// Controls default enrichments applied before <c>ReadFrom.Configuration</c>; sinks and levels remain in the
/// <c>Serilog</c> section and are installed by the consumer as separate NuGet packages.
/// </summary>
/// <example>
/// appsettings.json:
/// <code>
/// "RaccoonLandSerilog": {
///   "ApplicationName": "Ordering.Api",
///   "EnrichWithApplicationName": true,
///   "EnrichWithEnvironmentName": true,
///   "EnrichWithMachineName": true,
///   "EnrichWithTraceId": true
/// },
/// "Serilog": {
///   "Using": [ "Serilog.Sinks.Console" ],
///   "MinimumLevel": "Information",
///   "WriteTo": [ { "Name": "Console" } ]
/// }
/// </code>
/// </example>
public sealed class RaccoonLandSerilogOptions
{
    /// <summary>Default root configuration section name for RaccoonLand enrichments.</summary>
    public const string SectionName = "RaccoonLandSerilog";

    /// <summary>
    /// Logical application name written to every log event. When empty, falls back to
    /// <see cref="Microsoft.Extensions.Hosting.IHostEnvironment.ApplicationName"/>.
    /// </summary>
    public string ApplicationName { get; set; } = string.Empty;

    /// <summary>Adds an <c>ApplicationName</c> property to every log event.</summary>
    public bool EnrichWithApplicationName { get; set; } = true;

    /// <summary>Adds an <c>EnvironmentName</c> property to every log event.</summary>
    public bool EnrichWithEnvironmentName { get; set; } = true;

    /// <summary>Adds a <c>MachineName</c> property to every log event.</summary>
    public bool EnrichWithMachineName { get; set; } = true;

    /// <summary>
    /// Adds a <c>TraceId</c> property from <see cref="System.Diagnostics.Activity.Current"/> when present.
    /// </summary>
    public bool EnrichWithTraceId { get; set; } = true;
}
