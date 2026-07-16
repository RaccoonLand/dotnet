using Microsoft.Extensions.Options;
using RaccoonLand.Modules.FileStorage.Abstractions;
using RaccoonLand.Modules.FileStorage.S3.Configuration;
using RaccoonLand.Modules.FileStorage.S3.Internal;

namespace RaccoonLand.Modules.FileStorage.S3;

internal sealed class S3MultipartUploadCoordinator : IMultipartUploadCoordinator
{
    private readonly S3ObjectClient _client;
    private readonly S3ConnectionSettings _settings;
    private readonly FileStorageOptions _sharedOptions;

    public S3MultipartUploadCoordinator(
        S3ObjectClient client,
        IOptions<S3StorageOptions> options,
        IOptions<FileStorageOptions> sharedOptions)
    {
        _client = client;
        _settings = S3ConnectionSettings.FromOptions(options.Value);
        _sharedOptions = sharedOptions.Value;
    }

    public async Task<MultipartUploadSession> InitiateAsync(
        InitiateMultipartUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        FileStorageGuards.ValidateMultipartInitRequest(request, _sharedOptions);

        var key = StorageKey.NormalizeOrGenerate(request.Key);
        var uploadId = await _client.InitiateMultipartUploadAsync(
            _settings.ToObjectKey(key),
            request.ContentType,
            request.Metadata,
            cancellationToken);

        return new MultipartUploadSession(key, uploadId);
    }

    public async Task<UploadPartResult> UploadPartAsync(
        UploadPartRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.PartNumber <= 0)
        {
            throw new FileStorageValidationException("Part number must be greater than zero.");
        }

        var etag = await _client.UploadPartAsync(
            _settings.ToObjectKey(StorageKey.Normalize(request.Key)),
            request.UploadId,
            request.PartNumber,
            request.Content,
            request.ContentLength,
            cancellationToken);

        return new UploadPartResult(request.PartNumber, etag);
    }

    public async Task<FileRef> CompleteAsync(
        CompleteMultipartUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        var metadata = await _client.CompleteMultipartUploadAsync(
            _settings.ToObjectKey(StorageKey.Normalize(request.Key)),
            request.UploadId,
            request.Parts.Select(x => (x.PartNumber, x.ETag)).ToList(),
            cancellationToken);

        // ContentType/Length are not returned by S3 Complete; MIME is locked at Initiate.
        // Callers should use app session state from Initiate or GetMetadataAsync.
        return new FileRef
        {
            Key = StorageKey.Normalize(request.Key),
            Version = metadata.ETag,
            CreatedAtUtc = metadata.Timestamp,
        };
    }

    public async Task AbortAsync(AbortMultipartUploadRequest request, CancellationToken cancellationToken = default)
    {
        await _client.AbortMultipartUploadAsync(
            _settings.ToObjectKey(StorageKey.Normalize(request.Key)),
            request.UploadId,
            cancellationToken);
    }
}
