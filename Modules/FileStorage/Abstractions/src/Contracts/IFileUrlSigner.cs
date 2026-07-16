namespace RaccoonLand.Modules.FileStorage.Abstractions;

/// <summary>
/// Optional capability for providers that can issue direct HTTP read/write URLs (for example S3 pre-signed URLs).
/// </summary>
public interface IFileUrlSigner
{
    Task<SignedUrlResult> GetReadUrlAsync(SignedReadUrlRequest request, CancellationToken cancellationToken = default);

    Task<SignedUrlResult> GetWriteUrlAsync(SignedWriteUrlRequest request, CancellationToken cancellationToken = default);
}
