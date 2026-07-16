namespace RaccoonLand.Modules.FileStorage.Abstractions;

/// <summary>Metadata request.</summary>
public sealed class GetMetadataRequest
{
    public required string Key { get; init; }
}
