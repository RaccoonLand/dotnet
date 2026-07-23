using Serilog.Events;

namespace RaccoonLand.Modules.Observability.Logging.Serilog.Tests.Support;

internal static class LogEventExtensions
{
    public static bool HasProperty(this LogEvent logEvent, string name)
        => logEvent.Properties.ContainsKey(name);

    /// <summary>Returns the raw scalar value of a property, or null when absent/non-scalar.</summary>
    public static string? Scalar(this LogEvent logEvent, string name)
        => logEvent.Properties.TryGetValue(name, out var value) && value is ScalarValue scalar
            ? scalar.Value?.ToString()
            : null;
}
