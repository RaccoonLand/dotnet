using System.Diagnostics;
using System.Diagnostics.Metrics;
using RaccoonLand.Modules.Observability.Instrumentation.Telemetry;

namespace RaccoonLand.Modules.Observability.Instrumentation.Tests.Support;

/// <summary>Captures activities stopped on the RaccoonLand pipeline <see cref="ActivitySource"/>.</summary>
internal sealed class ActivityCollector : IDisposable
{
    private readonly ActivityListener _listener;

    public ActivityCollector()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == RaccoonLandTelemetry.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => Activities.Add(activity),
        };

        ActivitySource.AddActivityListener(_listener);
    }

    public List<Activity> Activities { get; } = [];

    public void Dispose() => _listener.Dispose();
}

/// <summary>A single captured metric measurement.</summary>
internal sealed record Measurement(string Instrument, double Value, IReadOnlyDictionary<string, object?> Tags);

/// <summary>Captures measurements published to the RaccoonLand pipeline <see cref="Meter"/>.</summary>
internal sealed class MetricCollector : IDisposable
{
    private readonly MeterListener _listener;

    public MetricCollector()
    {
        _listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == RaccoonLandTelemetry.MeterName)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            },
        };

        _listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => Record(instrument, value, tags));
        _listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) => Record(instrument, value, tags));
        _listener.Start();
    }

    public List<Measurement> Measurements { get; } = [];

    public IEnumerable<Measurement> For(string instrument)
        => Measurements.Where(measurement => measurement.Instrument == instrument);

    private void Record(Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var map = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var tag in tags)
        {
            map[tag.Key] = tag.Value;
        }

        Measurements.Add(new Measurement(instrument.Name, value, map));
    }

    public void Dispose() => _listener.Dispose();
}
