using RaccoonLand.Modules.Observability.Instrumentation.Configuration;
using RaccoonLand.Modules.Observability.Instrumentation.Diagnostics;
using RaccoonLand.Modules.Observability.Instrumentation.Tests.Support;

namespace RaccoonLand.Modules.Observability.Instrumentation.Tests.Diagnostics;

[Collection(TelemetryCollection.Name)]
public sealed class ToggleTests
{
    [Fact]
    public async Task InvokeAsync_WhenTracingDisabled_CreatesNoSpanButKeepsLogging()
    {
        using var activities = new ActivityCollector();
        var logger = new RecordingLogger<PipelineInstrumentationMiddleware>();
        var middleware = InstrumentationTestHelpers.CreateMiddleware(
            new InstrumentationOptions { EnableTracing = false },
            logger);

        await middleware.InvokeAsync(
            InstrumentationTestHelpers.CreateContext(),
            InstrumentationTestHelpers.Next());

        Assert.DoesNotContain(
            activities.Activities,
            a => a.DisplayName.Contains(nameof(SampleCommand), StringComparison.Ordinal));
        Assert.NotEmpty(logger.Entries);
    }

    [Fact]
    public async Task InvokeAsync_WhenMetricsDisabled_RecordsNoMeasurementsButKeepsTracing()
    {
        using var metrics = new MetricCollector();
        using var activities = new ActivityCollector();
        var middleware = InstrumentationTestHelpers.CreateMiddleware(
            new InstrumentationOptions { EnableMetrics = false });

        await middleware.InvokeAsync(
            InstrumentationTestHelpers.CreateContext(),
            InstrumentationTestHelpers.Next());

        Assert.Empty(metrics.Measurements);
        Assert.Contains(
            activities.Activities,
            a => a.DisplayName.Contains(nameof(SampleCommand), StringComparison.Ordinal));
    }

    [Fact]
    public async Task InvokeAsync_WhenLoggingDisabled_KeepsMetrics()
    {
        using var metrics = new MetricCollector();
        var logger = new RecordingLogger<PipelineInstrumentationMiddleware>();
        var middleware = InstrumentationTestHelpers.CreateMiddleware(
            new InstrumentationOptions { EnableLogging = false },
            logger);

        await middleware.InvokeAsync(
            InstrumentationTestHelpers.CreateContext(),
            InstrumentationTestHelpers.Next());

        Assert.Empty(logger.Entries);
        Assert.NotEmpty(metrics.For("raccoonland.requests.count"));
    }
}
