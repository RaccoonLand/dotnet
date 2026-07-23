using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace RaccoonLand.Modules.Security.Authentication.Tests.Support;

/// <summary>
/// Minimal configuration section that returns explicit children (for binder collision tests).
/// </summary>
internal sealed class FakeConfigurationSection : IConfigurationSection
{
    private readonly List<IConfigurationSection> _children = [];

    public FakeConfigurationSection(string key, string? value = null)
    {
        Key = key;
        Path = key;
        Value = value;
    }

    public string Key { get; }
    public string Path { get; }
    public string? Value { get; set; }

    public string? this[string key]
    {
        get => null;
        set { }
    }

    public void AddChild(IConfigurationSection child) => _children.Add(child);

    public IEnumerable<IConfigurationSection> GetChildren() => _children;

    public IChangeToken GetReloadToken() => NullChangeToken.Instance;

    public IConfigurationSection GetSection(string key)
        => _children.FirstOrDefault(c => string.Equals(c.Key, key, StringComparison.OrdinalIgnoreCase))
           ?? new FakeConfigurationSection(key);

    private sealed class NullChangeToken : IChangeToken
    {
        public static readonly NullChangeToken Instance = new();
        public bool HasChanged => false;
        public bool ActiveChangeCallbacks => false;
        public IDisposable RegisterChangeCallback(Action<object?> callback, object? state)
            => EmptyDisposable.Instance;
    }

    private sealed class EmptyDisposable : IDisposable
    {
        public static readonly EmptyDisposable Instance = new();
        public void Dispose() { }
    }
}
