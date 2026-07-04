namespace RaccoonLand.Modules.Observability.Instrumentation.Configuration;

/// <summary>
/// Toggles for the pipeline instrumentation middleware. Each pillar can be turned off independently; the
/// actual destination of the telemetry (exporters, sinks, sampling) is the host's responsibility.
/// </summary>
public sealed class InstrumentationOptions
{
    /// <summary>Default configuration section name (<c>Observability:Instrumentation</c>).</summary>
    public const string SectionName = "Observability:Instrumentation";

    /// <summary>When <see langword="true"/> a span is created per request via the shared <c>ActivitySource</c>. Default <see langword="true"/>.</summary>
    public bool EnableTracing { get; set; } = true;

    /// <summary>When <see langword="true"/> request count, duration and in-flight metrics are recorded. Default <see langword="true"/>.</summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// When <see langword="true"/> a structured start/finish log is written and an <c>ILogger</c> scope
    /// (correlation id, user id, tenant id, trace id) wraps the rest of the request. Default <see langword="true"/>.
    /// </summary>
    public bool EnableLogging { get; set; } = true;
}
