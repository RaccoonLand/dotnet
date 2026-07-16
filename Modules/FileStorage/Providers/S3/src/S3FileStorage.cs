using Microsoft.Extensions.Options;
using RaccoonLand.Modules.FileStorage.Abstractions;
using RaccoonLand.Modules.FileStorage.S3.Configuration;
using RaccoonLand.Modules.FileStorage.S3.Internal;

namespace RaccoonLand.Modules.FileStorage.S3;

internal sealed class S3FileStorage : IFileStorage
{
    private readonly S3ObjectClient _client;
    private readonly S3ConnectionSettings _settings;
    private readonly FileStorageOptions _sharedOptions;

    public S3FileStorage(
        S3ObjectClient client,
        IOptions<S3StorageOptions> options,
        IOptions<FileStorageOptions> sharedOptions)
    {
        _client = client;
        _settings = S3ConnectionSettings.FromOptions(options.Value);
        _sharedOptions = sharedOptions.Value;
    }

    public async Task<PutFileResult> PutAsync(PutFileRequest request, CancellationToken cancellationToken = default)
    {
        FileStorageGuards.ValidatePutRequest(request, _sharedOptions);

        var key = StorageKey.NormalizeOrGenerate(request.Key);
        var objectKey = _settings.ToObjectKey(key);

        var metadata = await _client.PutObjectAsync(
            objectKey,
            request.Content,
            request.ContentType,
            request.Metadata,
            request.ContentLength,
            createOnly: request.Mode is PutMode.CreateOnly,
            storageKey: key,
            cancellationToken);

        return new PutFileResult(ToFileRef(key, metadata));
    }

    public async Task<OpenReadResult> OpenReadAsync(OpenReadRequest request, CancellationToken cancellationToken = default)
    {
        var key = StorageKey.Normalize(request.Key);
        var objectKey = _settings.ToObjectKey(key);

        try
        {
            var (content, metadata) = await _client.GetObjectAsync(objectKey, cancellationToken);
            return new OpenReadResult
            {
                Content = content,
                File = ToFileRef(key, metadata),
            };
        }
        catch (FileNotFoundStorageException)
        {
            throw new FileNotFoundStorageException(key);
        }
    }

    public async Task DeleteAsync(DeleteFileRequest request, CancellationToken cancellationToken = default)
    {
        var key = StorageKey.Normalize(request.Key);
        var objectKey = _settings.ToObjectKey(key);

        try
        {
            await _client.DeleteObjectAsync(objectKey, cancellationToken);
        }
        catch (FileNotFoundStorageException) when (request.IgnoreNotFound)
        {
        }
    }

    public async Task<bool> ExistsAsync(ExistsFileRequest request, CancellationToken cancellationToken = default)
    {
        var key = StorageKey.Normalize(request.Key);
        return await _client.ObjectExistsAsync(_settings.ToObjectKey(key), cancellationToken);
    }

    public async Task<FileMetadata?> GetMetadataAsync(GetMetadataRequest request, CancellationToken cancellationToken = default)
    {
        var key = StorageKey.Normalize(request.Key);
        var metadata = await _client.HeadObjectAsync(_settings.ToObjectKey(key), cancellationToken);

        return metadata is null
            ? null
            : new FileMetadata { File = ToFileRef(key, metadata) };
    }

    private static FileRef ToFileRef(string key, S3ObjectMetadata metadata) => new()
    {
        Key = key,
        Version = metadata.ETag,
        Length = metadata.Length,
        ContentType = metadata.ContentType,
        CreatedAtUtc = metadata.Timestamp,
    };
}
