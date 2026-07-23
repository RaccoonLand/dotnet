using Microsoft.Extensions.Logging;
using RaccoonLand.Modules.Observability.Instrumentation.Configuration;
using RaccoonLand.Modules.Observability.Instrumentation.Diagnostics;
using RaccoonLand.Modules.Observability.Instrumentation.Tests.Support;

namespace RaccoonLand.Modules.Observability.Instrumentation.Tests.Diagnostics;

[Collection(TelemetryCollection.Name)]
public sealed class LoggingTests
{
    [Fact]
    public async Task InvokeAsync_WhenSuccess_WritesStartDebugAndFinishInformation()
    {
        var logger = new RecordingLogger<PipelineInstrumentationMiddleware>();
        var middleware = InstrumentationTestHelpers.CreateMiddleware(new InstrumentationOptions(), logger);

        await middleware.InvokeAsync(
            InstrumentationTestHelpers.CreateContext(),
            InstrumentationTestHelpers.Next());

        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Debug);
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Information && e.Message.Contains("success", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Entries, e => e.Level == LogLevel.Warning);
        Assert.DoesNotContain(logger.Entries, e => e.Level == LogLevel.Error);

        // Every log written during the request runs inside an enrichment scope (RequestKind is always present).
        var scope = Assert.Single(logger.Scopes);
        Assert.Equal("Command", scope["RequestKind"]);
    }

    [Fact]
    public async Task InvokeAsync_WhenFailure_WritesFinishWarning()
    {
        var logger = new RecordingLogger<PipelineInstrumentationMiddleware>();
        var middleware = InstrumentationTestHelpers.CreateMiddleware(new InstrumentationOptions(), logger);

        await middleware.InvokeAsync(
            InstrumentationTestHelpers.CreateContext(),
            InstrumentationTestHelpers.Next(InstrumentationTestHelpers.FailedResponse()));

        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("failure", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Entries, e => e.Level == LogLevel.Error);
    }

    [Fact]
    public async Task InvokeAsync_WhenException_WritesErrorOnceAndNoFinishEntry()
    {
        var logger = new RecordingLogger<PipelineInstrumentationMiddleware>();
        var middleware = InstrumentationTestHelpers.CreateMiddleware(new InstrumentationOptions(), logger);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.InvokeAsync(
                InstrumentationTestHelpers.CreateContext(),
                InstrumentationTestHelpers.Throwing(new InvalidOperationException("boom"))));

        Assert.Single(logger.Entries, e => e.Level == LogLevel.Error);
        // The catch block already logged the error; there must be no second "completed" finish entry.
        Assert.DoesNotContain(logger.Entries, e => e.Message.Contains("completed with", StringComparison.Ordinal));
    }

    [Fact]
    public async Task InvokeAsync_WhenLoggingDisabled_WritesNothing()
    {
        var logger = new RecordingLogger<PipelineInstrumentationMiddleware>();
        var middleware = InstrumentationTestHelpers.CreateMiddleware(
            new InstrumentationOptions { EnableLogging = false },
            logger);

        await middleware.InvokeAsync(
            InstrumentationTestHelpers.CreateContext(),
            InstrumentationTestHelpers.Next());

        Assert.Empty(logger.Entries);
        Assert.Empty(logger.Scopes);
    }
}
