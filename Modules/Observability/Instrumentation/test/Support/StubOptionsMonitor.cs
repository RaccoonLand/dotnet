using Microsoft.Extensions.Options;

namespace RaccoonLand.Modules.Observability.Instrumentation.Tests.Support;

/// <summary>
/// An <see cref="IOptionsMonitor{TOptions}"/> whose current value can be swapped or made to throw
/// <see cref="OptionsValidationException"/>, to exercise the middleware's snapshot/reload safety.
/// </summary>
internal sealed class StubOptionsMonitor<T>(T current) : IOptionsMonitor<T>
    where T : class
{
    private T _current = current;
    private Exception? _throw;

    public T CurrentValue => _throw is not null ? throw _throw : _current;

    public T Get(string? name) => CurrentValue;

    public IDisposable OnChange(Action<T, string?> listener) => new NoopDisposable();

    public void Set(T value)
    {
        _current = value;
        _throw = null;
    }

    public void ThrowOnRead(Exception exception) => _throw = exception;

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
