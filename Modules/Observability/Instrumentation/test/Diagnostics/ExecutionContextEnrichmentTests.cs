using RaccoonLand.Modules.Observability.Instrumentation.Configuration;
using RaccoonLand.Modules.Observability.Instrumentation.Diagnostics;
using RaccoonLand.Modules.Observability.Instrumentation.Tests.Support;

namespace RaccoonLand.Modules.Observability.Instrumentation.Tests.Diagnostics;

[Collection(TelemetryCollection.Name)]
public sealed class ExecutionContextEnrichmentTests
{
    private const string UserIdTag = "enduser.id";
    private const string TenantIdTag = "raccoonland.tenant.id";
    private const string CorrelationIdTag = "raccoonland.correlation_id";

    [Fact]
    public async Task InvokeAsync_WhenContextAvailable_EnrichesSpanTags()
    {
        using var activities = new ActivityCollector();
        var provider = InstrumentationTestHelpers.ProviderWith(new FakeExecutionContext
        {
            IsAvailable = true,
            UserId = "user-1",
            TenantId = "tenant-1",
            CorrelationId = "corr-1",
        });
        var middleware = InstrumentationTestHelpers.CreateMiddleware(new InstrumentationOptions());

        await middleware.InvokeAsync(
            InstrumentationTestHelpers.CreateContext(provider),
            InstrumentationTestHelpers.Next());

        var activity = activities.Activities.Single(
            a => a.DisplayName.Contains(nameof(SampleCommand), StringComparison.Ordinal));
        Assert.Equal("user-1", activity.GetTagItem(UserIdTag));
        Assert.Equal("tenant-1", activity.GetTagItem(TenantIdTag));
        Assert.Equal("corr-1", activity.GetTagItem(CorrelationIdTag));
    }

    [Fact]
    public async Task InvokeAsync_WhenContextAvailable_EnrichesLoggerScope()
    {
        var logger = new RecordingLogger<PipelineInstrumentationMiddleware>();
        var provider = InstrumentationTestHelpers.ProviderWith(new FakeExecutionContext
        {
            IsAvailable = true,
            UserId = "user-1",
            TenantId = "tenant-1",
            CorrelationId = "corr-1",
        });
        var middleware = InstrumentationTestHelpers.CreateMiddleware(new InstrumentationOptions(), logger);

        await middleware.InvokeAsync(
            InstrumentationTestHelpers.CreateContext(provider),
            InstrumentationTestHelpers.Next());

        var scope = Assert.Single(logger.Scopes);
        Assert.Equal("Command", scope["RequestKind"]);
        Assert.Equal("user-1", scope["UserId"]);
        Assert.Equal("tenant-1", scope["TenantId"]);
        Assert.Equal("corr-1", scope["CorrelationId"]);
    }

    [Fact]
    public async Task InvokeAsync_WhenTracingActive_AddsTraceIdToLoggerScope()
    {
        // With a listener present, the middleware's span is the current Activity when the scope opens, so the
        // scope must carry that span's trace id — this is what makes logs correlatable to traces.
        using var activities = new ActivityCollector();
        var logger = new RecordingLogger<PipelineInstrumentationMiddleware>();
        var middleware = InstrumentationTestHelpers.CreateMiddleware(new InstrumentationOptions(), logger);

        await middleware.InvokeAsync(
            InstrumentationTestHelpers.CreateContext(),
            InstrumentationTestHelpers.Next());

        var activity = activities.Activities.Single(
            a => a.DisplayName.Contains(nameof(SampleCommand), StringComparison.Ordinal));
        var scope = Assert.Single(logger.Scopes);
        Assert.Equal(activity.TraceId.ToString(), scope["TraceId"]);
    }

    [Fact]
    public async Task InvokeAsync_WhenContextUnavailable_DoesNotEnrich()
    {
        using var activities = new ActivityCollector();
        var logger = new RecordingLogger<PipelineInstrumentationMiddleware>();
        var provider = InstrumentationTestHelpers.ProviderWith(new FakeExecutionContext
        {
            IsAvailable = false,
            UserId = "user-1",
        });
        var middleware = InstrumentationTestHelpers.CreateMiddleware(new InstrumentationOptions(), logger);

        await middleware.InvokeAsync(
            InstrumentationTestHelpers.CreateContext(provider),
            InstrumentationTestHelpers.Next());

        var activity = activities.Activities.Single(
            a => a.DisplayName.Contains(nameof(SampleCommand), StringComparison.Ordinal));
        Assert.Null(activity.GetTagItem(UserIdTag));

        var scope = Assert.Single(logger.Scopes);
        Assert.False(scope.ContainsKey("UserId"));
        Assert.Equal("Command", scope["RequestKind"]);
    }

    [Fact]
    public async Task InvokeAsync_WhenNoExecutionContextService_DoesNotEnrich()
    {
        using var activities = new ActivityCollector();
        var middleware = InstrumentationTestHelpers.CreateMiddleware(new InstrumentationOptions());

        await middleware.InvokeAsync(
            InstrumentationTestHelpers.CreateContext(),
            InstrumentationTestHelpers.Next());

        var activity = activities.Activities.Single(
            a => a.DisplayName.Contains(nameof(SampleCommand), StringComparison.Ordinal));
        Assert.Null(activity.GetTagItem(UserIdTag));
        Assert.Null(activity.GetTagItem(TenantIdTag));
        Assert.Null(activity.GetTagItem(CorrelationIdTag));
    }
}
