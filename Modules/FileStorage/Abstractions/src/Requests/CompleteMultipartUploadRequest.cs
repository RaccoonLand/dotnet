namespace RaccoonLand.Modules.FileStorage.Abstractions;

/// <summary>Complete multipart upload request.</summary>
public sealed class CompleteMultipartUploadRequest
{
    public required string Key { get; init; }

    public required string UploadId { get; init; }

    public required IReadOnlyList<UploadPartResult> Parts { get; init; }
}
