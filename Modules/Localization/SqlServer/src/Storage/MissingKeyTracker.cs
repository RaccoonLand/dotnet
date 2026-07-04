using System.Collections.Concurrent;

namespace RaccoonLand.Modules.MessageLocalization.SQLServer.Storage;

/// <summary>A key that was requested but not found in the store, recorded for later persistence.</summary>
internal sealed record MissingKey(string Culture, string Key);

/// <summary>
/// Collects keys that were requested but missing. Reads never block on the database: the localizer just
/// records the miss here and the background refresh worker drains and persists them on its next cycle.
/// </summary>
internal sealed class MissingKeyTracker
{
    private readonly ConcurrentDictionary<MissingKey, byte> _pending = new();

    /// <summary>Records a missing key (deduplicated until the next drain).</summary>
    public void Report(string culture, string key) => _pending.TryAdd(new MissingKey(culture, key), 0);

    /// <summary>Atomically removes and returns all pending missing keys.</summary>
    public IReadOnlyCollection<MissingKey> Drain()
    {
        if (_pending.IsEmpty)
        {
            return [];
        }

        var keys = _pending.Keys.ToArray();
        foreach (var key in keys)
        {
            _pending.TryRemove(key, out _);
        }

        return keys;
    }
}
