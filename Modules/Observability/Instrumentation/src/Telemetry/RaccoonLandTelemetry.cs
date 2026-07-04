using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace RaccoonLand.Modules.Observability.Instrumentation.Telemetry;

/// <summary>
/// The well-known <see cref="ActivitySource"/> and <see cref="Meter"/> that the pipeline instrumentation
/// emits through. The framework only <i>produces</i> telemetry here; it never configures exporters. A host
/// opts in by registering these names with the OpenTelemetry SDK, for example:
/// <code>
/// builder.Services.AddOpenTelemetry()
///     .WithTracing(t => t.AddSource(RaccoonLandTelemetry.ActivitySourceName))
///     .WithMetrics(m => m.AddMeter(RaccoonLandTelemetry.MeterName));
/// </code>
/// </summary>
public static class RaccoonLandTelemetry
{
    /// <summary>Name of the <see cref="ActivitySource"/> used for pipeline spans.</summary>
    public const string ActivitySourceName = "RaccoonLand.RequestProcessing";

    /// <summary>Name of the <see cref="Meter"/> used for pipeline metrics.</summary>
    public const string MeterName = "RaccoonLand.RequestProcessing";

    private const string Version = "1.0.0";

    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName, Version);

    internal static readonly Meter Meter = new(MeterName, Version);

    /// <summary>Number of requests processed by the pipeline, tagged by kind, name and outcome.</summary>
    internal static readonly Counter<long> RequestCount = Meter.CreateCounter<long>(
        "raccoonland.requests.count",
        unit: "{request}",
        description: "Number of requests processed by the RaccoonLand pipeline.");

    /// <summary>Duration of pipeline request processing, in milliseconds.</summary>
    internal static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>(
        "raccoonland.requests.duration",
        unit: "ms",
        description: "Duration of RaccoonLand pipeline request processing.");

    /// <summary>Number of in-flight pipeline requests.</summary>
    internal static readonly UpDownCounter<long> ActiveRequests = Meter.CreateUpDownCounter<long>(
        "raccoonland.requests.active",
        unit: "{request}",
        description: "Number of in-flight RaccoonLand pipeline requests.");
}
