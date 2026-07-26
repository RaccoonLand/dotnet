namespace RaccoonLand.Modules.MessageLocalization.SQLServer.Storage;

/// <summary>A key that was requested but not found in the store, recorded for later persistence.</summary>
internal sealed record MissingKey(string Culture, string Key);

/// <summary>
/// The result of draining pending missing keys, plus the number of reports that were dropped since the last
/// drain because <see cref="MissingKeyTracker.Capacity"/> was reached. Callers log the drop count so operators
/// can spot a non-constant key ever leaking onto the lookup path.
/// </summary>
internal sealed record MissingKeyDrainResult(
    IReadOnlyCollection<MissingKey> Keys,
    long DroppedSinceLastDrain);

/// <summary>
/// Collects keys that were requested but missing. Reads never block on the database: the localizer just
/// records the miss here and the background refresh worker drains and persists them on its next cycle.
/// The pending set is bounded (<see cref="Capacity"/>) so a caller that ever passes non-constant strings as
/// message keys — a bug, since the module contract expects constants — cannot grow the buffer without bound.
/// </summary>
internal sealed class MissingKeyTracker
{
    /// <summary>Default upper bound on the pending set size.</summary>
    public const int DefaultCapacity = 10_000;

    private readonly object _gate = new();
    private readonly HashSet<MissingKey> _pending = [];
    private readonly int _capacity;
    private long _droppedSinceLastDrain;

    public MissingKeyTracker() : this(DefaultCapacity) { }

    public MissingKeyTracker(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacity),
                capacity,
                $"{nameof(MissingKeyTracker)} capacity must be greater than zero.");
        }

        _capacity = capacity;
    }

    /// <summary>Maximum number of unique missing keys the tracker will buffer between drains.</summary>
    public int Capacity => _capacity;

    /// <summary>
    /// Records a missing key (deduplicated until the next drain). When the pending set is already at
    /// <see cref="Capacity"/> and the (culture, key) pair is new, the report is dropped and counted; a
    /// duplicate of an already-tracked pair is always a cheap no-op.
    /// </summary>
    public void Report(string culture, string key)
    {
        var entry = new MissingKey(culture, key);

        lock (_gate)
        {
            if (_pending.Contains(entry))
            {
                // Already tracked — nothing to do, no capacity check needed.
                return;
            }

            if (_pending.Count >= _capacity)
            {
                _droppedSinceLastDrain++;
                return;
            }

            _pending.Add(entry);
        }
    }

    /// <summary>
    /// Removes and returns all pending missing keys under the same lock used by
    /// <see cref="Report"/> / <see cref="Requeue"/>, plus the number of reports dropped since the previous
    /// drain (the drop counter is reset).
    /// </summary>
    public MissingKeyDrainResult Drain()
    {
        lock (_gate)
        {
            var dropped = _droppedSinceLastDrain;
            _droppedSinceLastDrain = 0;

            if (_pending.Count == 0)
            {
                return new MissingKeyDrainResult([], dropped);
            }

            var keys = _pending.ToArray();
            _pending.Clear();
            return new MissingKeyDrainResult(keys, dropped);
        }
    }

    /// <summary>
    /// Puts keys back into the pending set (for example after a failed persist). Deduplicates with any
    /// keys reported since the corresponding <see cref="Drain"/>. Any entries that no longer fit under
    /// <see cref="Capacity"/> are counted in the next <see cref="Drain"/>'s drop count.
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
                if (_pending.Contains(key))
                {
                    continue;
                }

                if (_pending.Count >= _capacity)
                {
                    _droppedSinceLastDrain++;
                    continue;
                }

                _pending.Add(key);
            }
        }
    }
}
