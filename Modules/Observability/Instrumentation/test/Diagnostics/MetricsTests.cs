using RaccoonLand.Modules.Observability.Instrumentation.Configuration;
using RaccoonLand.Modules.Observability.Instrumentation.Tests.Support;

namespace RaccoonLand.Modules.Observability.Instrumentation.Tests.Diagnostics;

[Collection(TelemetryCollection.Name)]
public sealed class MetricsTests
{
    private const string CountInstrument = "raccoonland.requests.count";
    private const string DurationInstrument = "raccoonland.requests.duration";
    private const string ActiveInstrument = "raccoonland.requests.active";
    private const string NameTag = "raccoonland.request.name";
    private const string OutcomeTag = "raccoonland.request.outcome";
    private const string KindTag = "raccoonland.request.kind";

    [Fact]
    public async Task InvokeAsync_RecordsCountDurationAndActiveLifecycle()
    {
        using var metrics = new MetricCollector();
        var middleware = InstrumentationTestHelpers.CreateMiddleware(new InstrumentationOptions());

        await middleware.InvokeAsync(
            InstrumentationTestHelpers.CreateContext(),
            InstrumentationTestHelpers.Next());

        var count = Assert.Single(metrics.For(CountInstrument));
        Assert.Equal(1, count.Value);
        Assert.Equal("success", count.Tags[OutcomeTag]);
        Assert.Equal("Command", count.Tags[KindTag]);

        Assert.Single(metrics.For(DurationInstrument));

        var active = metrics.For(ActiveInstrument).ToList();
        Assert.Equal(2, active.Count);
        Assert.Equal(0d, active.Sum(m => m.Value));
    }

    [Fact]
    public async Task InvokeAsync_WhenFailure_TagsOutcomeFailureOnCount()
    {
        using var metrics = new MetricCollector();
        var middleware = InstrumentationTestHelpers.CreateMiddleware(new InstrumentationOptions());

        await middleware.InvokeAsync(
            InstrumentationTestHelpers.CreateContext(),
            InstrumentationTestHelpers.Next(InstrumentationTestHelpers.FailedResponse()));

        var count = Assert.Single(metrics.For(CountInstrument));
        Assert.Equal("failure", count.Tags[OutcomeTag]);
    }

    [Fact]
    public async Task InvokeAsync_WhenRequestNameNone_OmitsNameTag()
    {
        using var metrics = new MetricCollector();
        var middleware = InstrumentationTestHelpers.CreateMiddleware(
            new InstrumentationOptions { RequestNameInMetrics = RequestNameMetricTag.None });

        await middleware.InvokeAsync(
            InstrumentationTestHelpers.CreateContext(),
            InstrumentationTestHelpers.Next());

        var count = Assert.Single(metrics.For(CountInstrument));
        Assert.False(count.Tags.ContainsKey(NameTag));
    }

    [Fact]
    public async Task InvokeAsync_WhenRequestNameName_TagsWithShortName()
    {
        using var metrics = new MetricCollector();
        var middleware = InstrumentationTestHelpers.CreateMiddleware(
            new InstrumentationOptions { RequestNameInMetrics = RequestNameMetricTag.Name });

        await middleware.InvokeAsync(
            InstrumentationTestHelpers.CreateContext(),
            InstrumentationTestHelpers.Next());

        var count = Assert.Single(metrics.For(CountInstrument));
        Assert.Equal(nameof(SampleCommand), count.Tags[NameTag]);
    }

    [Fact]
    public async Task InvokeAsync_WhenRequestNameFullName_TagsWithFullName()
    {
        using var metrics = new MetricCollector();
        var middleware = InstrumentationTestHelpers.CreateMiddleware(
            new InstrumentationOptions { RequestNameInMetrics = RequestNameMetricTag.FullName });

        await middleware.InvokeAsync(
            InstrumentationTestHelpers.CreateContext(),
            InstrumentationTestHelpers.Next());

        var count = Assert.Single(metrics.For(CountInstrument));
        Assert.Equal(typeof(SampleCommand).FullName, count.Tags[NameTag]);
    }
}
