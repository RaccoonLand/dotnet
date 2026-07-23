using Microsoft.Extensions.Options;
using RaccoonLand.Modules.Observability.Instrumentation.Configuration;
using RaccoonLand.Modules.Observability.Instrumentation.Diagnostics;
using RaccoonLand.Modules.Observability.Instrumentation.Tests.Support;

namespace RaccoonLand.Modules.Observability.Instrumentation.Tests.Diagnostics;

[Collection(TelemetryCollection.Name)]
public sealed class ReloadSafetyTests
{
    [Fact]
    public async Task InvokeAsync_WhenOptionsReadThrows_UsesLastKnownGoodSnapshotValues()
    {
        // The last known-good snapshot has logging OFF and the short-name metric tag; both differ from the
        // type defaults (logging ON, FullName), so observing them proves the snapshot — not defaults — is used.
        var lastKnownGood = new InstrumentationOptions
        {
            EnableTracing = false,
            EnableMetrics = true,
            EnableLogging = false,
            RequestNameInMetrics = RequestNameMetricTag.Name,
        };
        var monitor = new StubOptionsMonitor<InstrumentationOptions>(lastKnownGood);
        var logger = new RecordingLogger<PipelineInstrumentationMiddleware>();
        var middleware = InstrumentationTestHelpers.CreateMiddleware(monitor, logger);
        using var metrics = new MetricCollector();

        // Simulate a reload that produced invalid options.
        monitor.ThrowOnRead(new OptionsValidationException(
            Options.DefaultName,
            typeof(InstrumentationOptions),
            ["invalid"]));

        var context = InstrumentationTestHelpers.CreateContext();
        await middleware.InvokeAsync(context, InstrumentationTestHelpers.Next());

        Assert.NotNull(context.Response);

        // EnableLogging=false came from the snapshot (default would have produced logs).
        Assert.Empty(logger.Entries);

        // RequestNameInMetrics=Name came from the snapshot (default FullName would tag the full type name).
        var count = Assert.Single(metrics.For("raccoonland.requests.count"));
        Assert.Equal(nameof(SampleCommand), count.Tags["raccoonland.request.name"]);
    }

    [Fact]
    public async Task InvokeAsync_WhenOptionsInvalidFromConstruction_FallsBackToDefaults()
    {
        var monitor = new StubOptionsMonitor<InstrumentationOptions>(new InstrumentationOptions());
        monitor.ThrowOnRead(new OptionsValidationException(
            Options.DefaultName,
            typeof(InstrumentationOptions),
            ["invalid"]));
        var middleware = InstrumentationTestHelpers.CreateMiddleware(monitor);

        var context = InstrumentationTestHelpers.CreateContext();
        var exception = await Record.ExceptionAsync(
            () => middleware.InvokeAsync(context, InstrumentationTestHelpers.Next()));

        Assert.Null(exception);
        Assert.NotNull(context.Response);
    }
}
