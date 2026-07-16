namespace RaccoonLand.Modules.FileStorage.Abstractions;

/// <summary>Streaming-first file storage contract. Authorization and ownership remain application concerns.</summary>
public interface IFileStorage
{
    Task<PutFileResult> PutAsync(PutFileRequest request, CancellationToken cancellationToken = default);

    Task<OpenReadResult> OpenReadAsync(OpenReadRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(DeleteFileRequest request, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(ExistsFileRequest request, CancellationToken cancellationToken = default);

    Task<FileMetadata?> GetMetadataAsync(GetMetadataRequest request, CancellationToken cancellationToken = default);
}
