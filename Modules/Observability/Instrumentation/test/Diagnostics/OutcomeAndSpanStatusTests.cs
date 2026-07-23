using System.Diagnostics;
using RaccoonLand.Modules.Observability.Instrumentation.Configuration;
using RaccoonLand.Modules.Observability.Instrumentation.Tests.Support;

namespace RaccoonLand.Modules.Observability.Instrumentation.Tests.Diagnostics;

[Collection(TelemetryCollection.Name)]
public sealed class OutcomeAndSpanStatusTests
{
    private const string OutcomeTag = "raccoonland.request.outcome";

    [Fact]
    public async Task InvokeAsync_WhenResponseHasNoErrors_TagsOutcomeSuccess()
    {
        using var activities = new ActivityCollector();
        var middleware = InstrumentationTestHelpers.CreateMiddleware(new InstrumentationOptions());
        var context = InstrumentationTestHelpers.CreateContext();

        await middleware.InvokeAsync(context, InstrumentationTestHelpers.Next());

        var activity = SingleActivity(activities);
        Assert.Equal("success", activity.GetTagItem(OutcomeTag));
        Assert.Equal(ActivityStatusCode.Unset, activity.Status);
    }

    [Fact]
    public async Task InvokeAsync_WhenResponseHasErrors_TagsFailureButDoesNotSetErrorStatus()
    {
        using var activities = new ActivityCollector();
        var middleware = InstrumentationTestHelpers.CreateMiddleware(new InstrumentationOptions());
        var context = InstrumentationTestHelpers.CreateContext();

        await middleware.InvokeAsync(
            context,
            InstrumentationTestHelpers.Next(InstrumentationTestHelpers.FailedResponse()));

        var activity = SingleActivity(activities);
        Assert.Equal("failure", activity.GetTagItem(OutcomeTag));
        Assert.Equal(ActivityStatusCode.Unset, activity.Status);
    }

    [Fact]
    public async Task InvokeAsync_WhenHandlerThrows_TagsExceptionSetsErrorStatusAndRethrows()
    {
        using var activities = new ActivityCollector();
        var middleware = InstrumentationTestHelpers.CreateMiddleware(new InstrumentationOptions());
        var context = InstrumentationTestHelpers.CreateContext();
        var boom = new InvalidOperationException("boom");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.InvokeAsync(context, InstrumentationTestHelpers.Throwing(boom)));

        var activity = SingleActivity(activities);
        Assert.Equal("exception", activity.GetTagItem(OutcomeTag));
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Contains(activity.Events, e => e.Name == "exception");
    }

    private static Activity SingleActivity(ActivityCollector collector)
        => collector.Activities.Single(a => a.DisplayName.Contains(nameof(SampleCommand), StringComparison.Ordinal));
}
