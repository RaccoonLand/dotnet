using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RaccoonLand.Core.ExecutionContext.Abstractions;
using RaccoonLand.Core.RequestProcessing.Abstractions.Pipeline;
using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;
using RaccoonLand.Modules.Observability.Instrumentation.Configuration;
using RaccoonLand.Modules.Observability.Instrumentation.Telemetry;

namespace RaccoonLand.Modules.Observability.Instrumentation.Diagnostics;

/// <summary>
/// A single pipeline middleware that instruments every command/query with the three observability pillars at
/// once: a distributed-tracing span, request metrics, and a structured log plus an <c>ILogger</c> scope that
/// carries the correlation id, user id, tenant id and trace id to every log written while the request runs.
/// <para>
/// It is exporter-agnostic — it only emits through <see cref="RaccoonLandTelemetry"/> (an
/// <see cref="ActivitySource"/> and a <c>Meter</c>) and <see cref="ILogger"/>. The host decides where the data
/// goes. Add it as the outermost middleware: <c>pipeline.UseMiddleware&lt;PipelineInstrumentationMiddleware&gt;()</c>.
/// </para>
/// </summary>
public sealed class PipelineInstrumentationMiddleware : IPipelineMiddleware
{
    private const string Success = "success";
    private const string Failure = "failure";
    private const string Exception = "exception";

    private readonly InstrumentationOptions _options;
    private readonly ILogger<PipelineInstrumentationMiddleware> _logger;

    public PipelineInstrumentationMiddleware(
        IOptions<InstrumentationOptions> options,
        ILogger<PipelineInstrumentationMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(PipelineContext context, PipelineDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var requestType = context.Request.GetType();
        var requestName = requestType.FullName ?? requestType.Name;
        var kind = context.Kind.ToString();

        using var activity = _options.EnableTracing
            ? RaccoonLandTelemetry.ActivitySource.StartActivity($"{kind} {requestName}", ActivityKind.Internal)
            : null;

        if (activity is not null)
        {
            activity.SetTag(Tags.RequestKind, kind);
            activity.SetTag(Tags.RequestName, requestName);
            EnrichWithExecutionContext(activity, context);
        }

        using var scope = _options.EnableLogging ? BeginExecutionScope(context, kind) : null;
        if (_options.EnableLogging)
        {
            _logger.LogDebug("Handling {RequestKind} {RequestName}.", kind, requestName);
        }

        var activeTags = new TagList { { Tags.RequestKind, kind } };
        if (_options.EnableMetrics)
        {
            RaccoonLandTelemetry.ActiveRequests.Add(1, activeTags);
        }

        var startTimestamp = Stopwatch.GetTimestamp();
        var outcome = Success;

        try
        {
            await next(context);

            outcome = DetermineOutcome(context.Response);
            activity?.SetTag(Tags.Outcome, outcome);
        }
        catch (Exception exception)
        {
            outcome = Exception;
            if (activity is not null)
            {
                activity.SetTag(Tags.Outcome, outcome);
                activity.SetStatus(ActivityStatusCode.Error, exception.Message);
                activity.AddException(exception);
            }

            _logger.LogError(
                exception,
                "{RequestKind} {RequestName} threw {ExceptionType}.",
                kind,
                requestName,
                exception.GetType().Name);

            throw;
        }
        finally
        {
            var elapsedMs = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;

            if (_options.EnableMetrics)
            {
                RaccoonLandTelemetry.ActiveRequests.Add(-1, activeTags);
                RecordMetrics(kind, requestName, outcome, elapsedMs);
            }

            if (_options.EnableLogging && outcome != Exception)
            {
                Log(kind, requestName, outcome, elapsedMs);
            }
        }
    }

    private void Log(string kind, string requestName, string outcome, double elapsedMs)
    {
        if (outcome == Failure)
        {
            _logger.LogWarning(
                "{RequestKind} {RequestName} completed with {Outcome} in {ElapsedMilliseconds:0.###} ms.",
                kind, requestName, outcome, elapsedMs);
        }
        else
        {
            _logger.LogInformation(
                "{RequestKind} {RequestName} completed with {Outcome} in {ElapsedMilliseconds:0.###} ms.",
                kind, requestName, outcome, elapsedMs);
        }
    }

    private static void RecordMetrics(string kind, string requestName, string outcome, double elapsedMs)
    {
        var tags = new TagList
        {
            { Tags.RequestKind, kind },
            { Tags.RequestName, requestName },
            { Tags.Outcome, outcome },
        };

        RaccoonLandTelemetry.RequestCount.Add(1, tags);
        RaccoonLandTelemetry.RequestDuration.Record(elapsedMs, tags);
    }

    private static string DetermineOutcome(PipelineResponse? response)
        => response is { Errors.Count: > 0 } ? Failure : Success;

    private static void EnrichWithExecutionContext(Activity activity, PipelineContext context)
    {
        var execution = context.RequestServices.GetService<ICurrentExecutionContext>();
        if (execution is not { IsAvailable: true })
        {
            return;
        }

        SetIfPresent(activity, Tags.UserId, execution.UserId);
        SetIfPresent(activity, Tags.TenantId, execution.TenantId);
        SetIfPresent(activity, Tags.CorrelationId, execution.CorrelationId);
    }

    private static void SetIfPresent(Activity activity, string key, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            activity.SetTag(key, value);
        }
    }

    private IDisposable? BeginExecutionScope(PipelineContext context, string kind)
    {
        var state = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["RequestKind"] = kind,
        };

        var traceId = Activity.Current?.TraceId.ToString();
        if (!string.IsNullOrEmpty(traceId))
        {
            state["TraceId"] = traceId;
        }

        var execution = context.RequestServices.GetService<ICurrentExecutionContext>();
        if (execution is { IsAvailable: true })
        {
            AddIfPresent(state, "UserId", execution.UserId);
            AddIfPresent(state, "TenantId", execution.TenantId);
            AddIfPresent(state, "CorrelationId", execution.CorrelationId);
        }

        return _logger.BeginScope(state);
    }

    private static void AddIfPresent(IDictionary<string, object?> state, string key, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            state[key] = value;
        }
    }

    private static class Tags
    {
        public const string RequestKind = "raccoonland.request.kind";
        public const string RequestName = "raccoonland.request.name";
        public const string Outcome = "raccoonland.request.outcome";
        public const string UserId = "enduser.id";
        public const string TenantId = "raccoonland.tenant.id";
        public const string CorrelationId = "raccoonland.correlation_id";
    }
}
