using Serilog.Core;
using Serilog.Events;

namespace RaccoonLand.Modules.Observability.Logging.Serilog.Tests.Support;

/// <summary>
/// An in-memory Serilog sink. Registered in DI so the extension's <c>ReadFrom.Services</c> wires it into the
/// built logger, letting tests assert on the fully enriched <see cref="LogEvent"/>s.
/// </summary>
internal sealed class InMemorySink : ILogEventSink
{
    private readonly object _gate = new();
    private readonly List<LogEvent> _events = [];

    public IReadOnlyList<LogEvent> Events
    {
        get
        {
            lock (_gate)
            {
                return _events.ToList();
            }
        }
    }

    public void Emit(LogEvent logEvent)
    {
        lock (_gate)
        {
            _events.Add(logEvent);
        }
    }
}
