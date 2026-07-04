using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace RaccoonLand.Modules.Observability.Logging.Serilog.Enrichers;

/// <summary>
/// Adds the current <see cref="Activity.TraceId"/> as a <c>TraceId</c> log property when an activity exists.
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
