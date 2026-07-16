namespace RaccoonLand.Modules.FileStorage.Abstractions;

/// <summary>Exists request.</summary>
public sealed class ExistsFileRequest
{
    public required string Key { get; init; }
}
