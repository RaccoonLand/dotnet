namespace RaccoonLand.Modules.FileStorage.Abstractions;

/// <summary>Multipart upload capability for large direct-to-storage uploads.</summary>
public interface IMultipartUploadCoordinator
{
    Task<MultipartUploadSession> InitiateAsync(
        InitiateMultipartUploadRequest request,
        CancellationToken cancellationToken = default);

    Task<UploadPartResult> UploadPartAsync(
        UploadPartRequest request,
        CancellationToken cancellationToken = default);

    Task<FileRef> CompleteAsync(
        CompleteMultipartUploadRequest request,
        CancellationToken cancellationToken = default);

    Task AbortAsync(AbortMultipartUploadRequest request, CancellationToken cancellationToken = default);
}
