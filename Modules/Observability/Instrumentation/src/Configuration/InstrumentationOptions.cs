namespace RaccoonLand.Modules.Observability.Instrumentation.Configuration;

/// <summary>
/// Toggles for the pipeline instrumentation middleware. Each pillar can be turned off independently; the
/// actual destination of the telemetry (exporters, sinks, sampling) is the host's responsibility.
/// Options are read via <c>IOptionsMonitor&lt;InstrumentationOptions&gt;</c> at the start of each request,
/// so changes from a reloadable configuration source apply to subsequent requests without restarting the host.
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
    /// When <see langword="true"/> a structured start log, finish log (or exception log), and an <c>ILogger</c>
    /// scope (correlation id, user id, tenant id, trace id) are written for the request. When
    /// <see langword="false"/>, this middleware emits no logs at all (including on exceptions). Default
    /// <see langword="true"/>.
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Controls whether request count/duration metrics include <c>raccoonland.request.name</c>.
    /// Defaults to <see cref="RequestNameMetricTag.FullName"/> so requests with the same short type name in
    /// different namespaces do not collide. Use <see cref="RequestNameMetricTag.Name"/> or
    /// <see cref="RequestNameMetricTag.None"/> when metric cardinality must be reduced.
    /// Spans always use <see cref="Type.FullName"/> for the request name.
    /// </summary>
    public RequestNameMetricTag RequestNameInMetrics { get; set; } = RequestNameMetricTag.FullName;
}
