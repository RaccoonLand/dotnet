using Serilog.Core;
using Serilog.Events;

namespace RaccoonLand.Modules.Observability.Logging.Serilog.Tests.Support;

/// <summary>Minimal <see cref="ILogEventPropertyFactory"/> that wraps values in scalar properties.</summary>
internal sealed class TestPropertyFactory : ILogEventPropertyFactory
{
    public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
        => new(name, new ScalarValue(value));
}
