using RaccoonLand.Modules.FileStorage.Abstractions;

namespace RaccoonLand.Modules.FileStorage.FileSystem.Internal;

internal sealed class StoredMetadata
{
    public required string Key { get; init; }

    public string? ContentType { get; init; }

    public long Length { get; init; }

    public string? ChecksumSha256 { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public Dictionary<string, string>? UserMetadata { get; init; }

    public FileRef ToFileRef() => new()
    {
        Key = Key,
        Version = ChecksumSha256,
        Length = Length,
        ContentType = ContentType,
        ChecksumSha256 = ChecksumSha256,
        CreatedAtUtc = CreatedAtUtc,
    };
}
