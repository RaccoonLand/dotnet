using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace RaccoonLand.Modules.Observability.Logging.Serilog.Enrichers;

/// <summary>
/// Adds the current <see cref="Activity.TraceId"/> as a log property named <c>TraceId</c> when an activity
/// exists. Uses Serilog's add-if-absent semantics so an existing <c>TraceId</c> on the event is not replaced by
/// this enricher. Global collision/override behaviour still depends on other enrichers the consumer registers.
/// Backend-specific field names (for example <c>trace_id</c>) are the sink/host's responsibility.
/// </summary>
internal sealed class TraceIdEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var traceId = Activity.Current?.TraceId.ToString();
        if (!string.IsNullOrEmpty(traceId))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TraceId", traceId));
        }
    }
}
