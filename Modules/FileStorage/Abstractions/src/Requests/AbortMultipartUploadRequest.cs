namespace RaccoonLand.Modules.FileStorage.Abstractions;

/// <summary>Abort multipart upload request.</summary>
public sealed class AbortMultipartUploadRequest
{
    public required string Key { get; init; }

    public required string UploadId { get; init; }
}
