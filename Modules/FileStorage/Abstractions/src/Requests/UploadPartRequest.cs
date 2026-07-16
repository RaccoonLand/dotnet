namespace RaccoonLand.Modules.FileStorage.Abstractions;

/// <summary>Upload part request.</summary>
public sealed class UploadPartRequest
{
    public required string Key { get; init; }

    public required string UploadId { get; init; }

    public required int PartNumber { get; init; }

    public required Stream Content { get; init; }

    public long? ContentLength { get; init; }
}
