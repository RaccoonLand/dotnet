namespace RaccoonLand.Modules.MessageLocalization.SQLServer.Storage;

/// <summary>A key that was requested but not found in the store, recorded for later persistence.</summary>
internal sealed record MissingKey(string Culture, string Key);

/// <summary>
/// Collects keys that were requested but missing. Reads never block on the database: the localizer just
/// records the miss here and the background refresh worker drains and persists them on its next cycle.
/// </summary>
internal sealed class MissingKeyTracker
{
    private readonly object _gate = new();
    private readonly HashSet<MissingKey> _pending = [];

    /// <summary>Records a missing key (deduplicated until the next drain).</summary>
    public void Report(string culture, string key)
    {
        lock (_gate)
        {
            _pending.Add(new MissingKey(culture, key));
        }
    }

    /// <summary>
    /// Removes and returns all pending missing keys under the same lock used by
    /// <see cref="Report"/> / <see cref="Requeue"/>.
    /// </summary>
    public IReadOnlyCollection<MissingKey> Drain()
    {
        lock (_gate)
        {
            if (_pending.Count == 0)
            {
                return [];
            }

            var keys = _pending.ToArray();
            _pending.Clear();
            return keys;
        }
    }

    /// <summary>
    /// Puts keys back into the pending set (for example after a failed persist). Deduplicates with any
    /// keys reported since the corresponding <see cref="Drain"/>.
    /// </summary>
    public void Requeue(IEnumerable<MissingKey> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);

        // Materialize before taking the lock so deferred enumerables cannot run under the lock.
        var materialized = keys as IReadOnlyCollection<MissingKey> ?? keys.ToArray();
        if (materialized.Count == 0)
        {
            return;
        }

        lock (_gate)
        {
            foreach (var key in materialized)
            {
                _pending.Add(key);
            }
        }
    }
}
