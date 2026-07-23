using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;
using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;
using RaccoonLand.Modules.Observability.Instrumentation.Configuration;
using RaccoonLand.Modules.Observability.Instrumentation.Diagnostics;

namespace RaccoonLand.Modules.Observability.Instrumentation.Tests.Support;

internal static class InstrumentationTestHelpers
{
    public static PipelineInstrumentationMiddleware CreateMiddleware(
        InstrumentationOptions options,
        ILogger<PipelineInstrumentationMiddleware>? logger = null)
        => new(new StubOptionsMonitor<InstrumentationOptions>(options), logger ?? NullLoggerOf());

    public static PipelineInstrumentationMiddleware CreateMiddleware(
        IOptionsMonitor<InstrumentationOptions> monitor,
        ILogger<PipelineInstrumentationMiddleware>? logger = null)
        => new(monitor, logger ?? NullLoggerOf());

    public static PipelineContext CreateContext(
        IServiceProvider? services = null,
        IRequestBase? request = null,
        RequestKind kind = RequestKind.Command)
        => new(request ?? new SampleCommand(), kind, services ?? EmptyServiceProvider.Instance);

    /// <summary>A terminal delegate that sets a (possibly failing) response.</summary>
    public static PipelineDelegate Next(PipelineResponse? response = null)
        => context =>
        {
            context.Response = response ?? new PipelineResponse { Result = "ok" };
            return Task.CompletedTask;
        };

    /// <summary>A terminal delegate that throws.</summary>
    public static PipelineDelegate Throwing(Exception exception)
        => _ => throw exception;

    public static PipelineResponse FailedResponse()
        => new() { Errors = [new PipelineMessage("ERR", "boom")] };

    public static IServiceProvider ProviderWith(RaccoonLand.Core.ExecutionContext.Abstractions.ICurrentExecutionContext execution)
    {
        var services = new ServiceCollection();
        services.AddSingleton(execution);
        return services.BuildServiceProvider();
    }

    private static ILogger<PipelineInstrumentationMiddleware> NullLoggerOf()
        => new RecordingLogger<PipelineInstrumentationMiddleware>();

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public static EmptyServiceProvider Instance { get; } = new();

        public object? GetService(Type serviceType) => null;
    }
}
