namespace RaccoonLand.Modules.Observability.Instrumentation.Tests.Support;

/// <summary>
/// Serializes the middleware tests. They observe the process-wide static <c>ActivitySource</c>/<c>Meter</c>
/// via listeners, so running them in parallel would let one test capture another's telemetry.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class TelemetryCollection
{
    public const string Name = "Instrumentation telemetry";
}
