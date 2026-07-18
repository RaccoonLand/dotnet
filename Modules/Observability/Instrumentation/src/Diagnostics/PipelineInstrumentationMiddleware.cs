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
/// <para>
/// Options are read from <see cref="IOptionsMonitor{TOptions}"/> at the start of each request so reloadable
/// configuration applies without restarting the host. A single snapshot is used for the duration of that request.
/// If a reload produces invalid options (<see cref="OptionsValidationException"/>), the middleware keeps using
/// the last known-good snapshot so the business request is never failed by observability configuration.
/// </para>
/// </summary>
public sealed class PipelineInstrumentationMiddleware : IPipelineMiddleware
{
    private const string Success = "success";
    private const string Failure = "failure";
    private const string Exception = "exception";

    private readonly IOptionsMonitor<InstrumentationOptions> _options;
    private readonly ILogger<PipelineInstrumentationMiddleware> _logger;
    private InstrumentationOptions _effective;

    public PipelineInstrumentationMiddleware(
        IOptionsMonitor<InstrumentationOptions> options,
        ILogger<PipelineInstrumentationMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options;
        _logger = logger;
        _effective = TryReadOptions(options) ?? new InstrumentationOptions();
    }

    public async Task InvokeAsync(PipelineContext context, PipelineDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var options = ResolveOptions();
        var requestType = context.Request.GetType();
        var requestFullName = requestType.FullName ?? requestType.Name;
        var kind = context.Kind.ToString();

        using var activity = options.EnableTracing
            ? RaccoonLandTelemetry.ActivitySource.StartActivity($"{kind} {requestFullName}", ActivityKind.Internal)
            : null;

        if (activity is not null)
        {
            activity.SetTag(Tags.RequestKind, kind);
            activity.SetTag(Tags.RequestName, requestFullName);
            EnrichWithExecutionContext(activity, context);
        }

        using var scope = options.EnableLogging ? BeginExecutionScope(context, kind) : null;
        if (options.EnableLogging)
        {
            _logger.LogDebug("Handling {RequestKind} {RequestName}.", kind, requestFullName);
        }

        var activeTags = new TagList { { Tags.RequestKind, kind } };
        if (options.EnableMetrics)
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
                // Avoid putting exception.Message on the status description — it may contain sensitive data.
                // Full exception details still go through AddException for exporters that consume events.
                activity.SetStatus(
                    ActivityStatusCode.Error,
                    "An unhandled exception occurred during request processing.");
                activity.AddException(exception);
            }

            if (options.EnableLogging)
            {
                _logger.LogError(
                    exception,
                    "{RequestKind} {RequestName} threw {ExceptionType}.",
                    kind,
                    requestFullName,
                    exception.GetType().Name);
            }

            throw;
        }
        finally
        {
            var elapsedMs = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;

            if (options.EnableMetrics)
            {
                RaccoonLandTelemetry.ActiveRequests.Add(-1, activeTags);
                RecordMetrics(options.RequestNameInMetrics, kind, requestType, outcome, elapsedMs);
            }

            // On the exception path the catch block already wrote LogError; skip a second finish entry.
            if (options.EnableLogging && outcome != Exception)
            {
                Log(kind, requestFullName, outcome, elapsedMs);
            }
        }
    }

    private InstrumentationOptions ResolveOptions()
    {
        try
        {
            var current = _options.CurrentValue;
            Volatile.Write(ref _effective, current);
            return current;
        }
        catch (OptionsValidationException)
        {
            return Volatile.Read(ref _effective);
        }
    }

    private static InstrumentationOptions? TryReadOptions(IOptionsMonitor<InstrumentationOptions> options)
    {
        try
        {
            return options.CurrentValue;
        }
        catch (OptionsValidationException)
        {
            return null;
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

    private static void RecordMetrics(
        RequestNameMetricTag nameTag,
        string kind,
        Type requestType,
        string outcome,
        double elapsedMs)
    {
        var tags = new TagList
        {
            { Tags.RequestKind, kind },
            { Tags.Outcome, outcome },
        };

        var metricName = ResolveMetricRequestName(nameTag, requestType);
        if (metricName is not null)
        {
            tags.Add(Tags.RequestName, metricName);
        }

        RaccoonLandTelemetry.RequestCount.Add(1, tags);
        RaccoonLandTelemetry.RequestDuration.Record(elapsedMs, tags);
    }

    private static string? ResolveMetricRequestName(RequestNameMetricTag nameTag, Type requestType)
        => nameTag switch
        {
            RequestNameMetricTag.None => null,
            RequestNameMetricTag.Name => requestType.Name,
            // FullName, and any undefined enum value: never fail the business request for bad telemetry config.
            _ => requestType.FullName ?? requestType.Name,
        };

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
