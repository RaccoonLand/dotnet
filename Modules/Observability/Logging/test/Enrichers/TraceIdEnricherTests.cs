using System.Diagnostics;
using RaccoonLand.Modules.Observability.Logging.Serilog.Enrichers;
using RaccoonLand.Modules.Observability.Logging.Serilog.Tests.Support;
using Serilog.Events;

namespace RaccoonLand.Modules.Observability.Logging.Serilog.Tests.Enrichers;

public sealed class TraceIdEnricherTests
{
    private readonly TraceIdEnricher _enricher = new();
    private readonly TestPropertyFactory _factory = new();

    [Fact]
    public void Enrich_WhenActivityCurrent_AddsTraceIdEqualToActivityTraceId()
    {
        using var activity = new Activity("test").Start();
        var logEvent = TestLogEvent.Create();

        _enricher.Enrich(logEvent, _factory);

        Assert.True(logEvent.HasProperty("TraceId"));
        Assert.Equal(activity.TraceId.ToString(), logEvent.Scalar("TraceId"));
    }

    [Fact]
    public void Enrich_WhenEventAlreadyHasTraceId_DoesNotOverwrite()
    {
        using var activity = new Activity("test").Start();
        var existing = new LogEventProperty("TraceId", new ScalarValue("existing-trace-id"));
        var logEvent = TestLogEvent.Create(existing);

        _enricher.Enrich(logEvent, _factory);

        // Sanity: the activity's id differs, so a naive overwrite would be observable.
        Assert.NotEqual("existing-trace-id", activity.TraceId.ToString());
        Assert.Equal("existing-trace-id", logEvent.Scalar("TraceId"));
    }

    [Fact]
    public void Enrich_WhenNoActivity_AddsNothing()
    {
        Assert.Null(Activity.Current);
        var logEvent = TestLogEvent.Create();

        _enricher.Enrich(logEvent, _factory);

        Assert.False(logEvent.HasProperty("TraceId"));
    }
}
