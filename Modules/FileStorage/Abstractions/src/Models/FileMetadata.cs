namespace RaccoonLand.Modules.FileStorage.Abstractions;

/// <summary>Metadata returned by storage providers without opening the full content stream.</summary>
public sealed record FileMetadata
{
    public required FileRef File { get; init; }

    public IReadOnlyDictionary<string, string>? UserMetadata { get; init; }
}
