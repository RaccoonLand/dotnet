using Serilog.Events;
using Serilog.Parsing;

namespace RaccoonLand.Modules.Observability.Logging.Serilog.Tests.Support;

internal static class TestLogEvent
{
    /// <summary>Builds an <see cref="LogEvent"/> at Information level with the given (optional) properties.</summary>
    public static LogEvent Create(params LogEventProperty[] properties)
        => new(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            exception: null,
            new MessageTemplateParser().Parse("test"),
            properties);
}
