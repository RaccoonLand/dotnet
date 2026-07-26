using Microsoft.Extensions.Logging;

namespace RaccoonLand.Modules.MessageLocalization.SQLServer.Tests.Support;

/// <summary>
/// Minimal in-memory <see cref="ILogger{T}"/> used by tests to capture emitted log entries. Not thread-safe;
/// tests using it stay on the calling thread.
/// </summary>
internal sealed class ListLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message)> Entries { get; } = [];

    IDisposable? ILogger.BeginScope<TState>(TState state) => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Entries.Add((logLevel, formatter(state, exception)));
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
