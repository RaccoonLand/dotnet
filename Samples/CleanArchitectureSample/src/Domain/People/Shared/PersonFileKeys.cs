namespace CleanArchitectureSample.People.Shared;

/// <summary>
/// Deterministic FileStorage keys and metadata for Person-owned objects.
/// Keys use BusinessKey (not database Id) so files can be written before insert and orphans can be correlated later.
/// </summary>
public static class PersonFileKeys
{
    public const string AggregateName = "Person";

    public const string AggregateMetadataKey = "aggregate";

    public const string BusinessKeyMetadataKey = "business-key";

    public static string Photo(Guid businessKey) => $"person_{businessKey:N}_photo";

    public static string Resume(Guid businessKey) => $"person_{businessKey:N}_resume";

    public static IReadOnlyDictionary<string, string> CreateMetadata(Guid businessKey)
        => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [AggregateMetadataKey] = AggregateName,
            [BusinessKeyMetadataKey] = businessKey.ToString("N"),
        };
}
