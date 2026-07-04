namespace RaccoonLand.Modules.MessageLocalization.SQLServer.Storage;

/// <summary>
/// Thread-safe in-memory snapshot of all translations for the current application. The whole set is loaded
/// at startup and atomically swapped on each periodic refresh, so reads are lock-free and always consistent.
/// </summary>
internal sealed class MessageLocalizationStore
{
    // Outer key: culture name (case-insensitive). Inner key: message/parameter key (case-sensitive).
    private volatile IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _snapshot =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Number of cultures currently held in the snapshot.</summary>
    public int CultureCount => _snapshot.Count;

    /// <summary>Atomically replaces the whole snapshot with the supplied entries.</summary>
    public void Replace(IEnumerable<LocalizationEntry> entries)
    {
        var byCulture = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            if (!byCulture.TryGetValue(entry.Culture, out var map))
            {
                map = new Dictionary<string, string>(StringComparer.Ordinal);
                byCulture[entry.Culture] = map;
            }

            map[entry.Key] = entry.Value;
        }

        var snapshot = new Dictionary<string, IReadOnlyDictionary<string, string>>(byCulture.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var pair in byCulture)
        {
            snapshot[pair.Key] = pair.Value;
        }

        _snapshot = snapshot;
    }

    /// <summary>Returns the value for an exact (culture, key) pair, if present.</summary>
    public bool TryGet(string culture, string key, out string value)
    {
        if (_snapshot.TryGetValue(culture, out var map) && map.TryGetValue(key, out var found))
        {
            value = found;
            return true;
        }

        value = string.Empty;
        return false;
    }
}
