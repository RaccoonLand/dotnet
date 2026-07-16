using Microsoft.Extensions.Options;
using RaccoonLand.Modules.FileStorage.Abstractions;
using RaccoonLand.Modules.FileStorage.FileSystem.Configuration;
using RaccoonLand.Modules.FileStorage.FileSystem.Internal;

namespace RaccoonLand.Modules.FileStorage.FileSystem;

internal sealed class FileSystemFileStorage : IFileStorage
{
    private readonly FileSystemPathLayout _paths;
    private readonly FileSystemMetadataStore _metadata;
    private readonly FileStorageOptions _sharedOptions;

    public FileSystemFileStorage(
        IOptions<FileSystemStorageOptions> options,
        IOptions<FileStorageOptions> sharedOptions)
    {
        _paths = new FileSystemPathLayout(options.Value.RootPath);
        _metadata = new FileSystemMetadataStore(_paths);
        _sharedOptions = sharedOptions.Value;
    }

    public async Task<PutFileResult> PutAsync(PutFileRequest request, CancellationToken cancellationToken = default)
    {
        FileStorageGuards.ValidatePutRequest(request, _sharedOptions);

        var key = StorageKey.NormalizeOrGenerate(request.Key);
        var targetPath = _paths.GetObjectPath(key);
        var metadataPath = _paths.GetMetadataPath(key);

        var writeResult = await FileSystemAtomicFileWriter.WriteToTempAsync(
            request.Content,
            targetPath,
            cancellationToken);

        if (_sharedOptions.MaxUploadBytes is long maxBytes && writeResult.Length > maxBytes)
        {
            FileSystemAtomicFileWriter.Discard(writeResult.TempPath);
            throw new FileStorageValidationException($"Upload exceeds the configured limit of {maxBytes} bytes.");
        }

        var storedMetadata = new StoredMetadata
        {
            Key = key,
            ContentType = request.ContentType,
            Length = writeResult.Length,
            ChecksumSha256 = writeResult.ChecksumSha256,
            CreatedAtUtc = await _metadata.ResolveCreatedAtUtcAsync(key, request.Mode, cancellationToken),
            UserMetadata = request.Metadata?.ToDictionary(StringComparer.OrdinalIgnoreCase),
        };

        string? metadataTempPath = await _metadata.WriteToTempAsync(storedMetadata, cancellationToken);
        string? objectTempPath = writeResult.TempPath;

        try
        {
            FileSystemAtomicFileWriter.Commit(objectTempPath, targetPath, request.Mode);
            objectTempPath = null;
        }
        catch
        {
            if (metadataTempPath is not null)
            {
                FileSystemMetadataStore.Discard(metadataTempPath);
            }

            if (objectTempPath is not null)
            {
                FileSystemAtomicFileWriter.Discard(objectTempPath);
            }

            throw;
        }

        try
        {
            FileSystemMetadataStore.Commit(metadataTempPath, metadataPath);
            metadataTempPath = null;
        }
        catch when (request.Mode is not PutMode.CreateOnly)
        {
            // Metadata sidecar is best-effort on replace. The object is already committed.
            if (metadataTempPath is not null)
            {
                FileSystemMetadataStore.Discard(metadataTempPath);
            }

            return new PutFileResult(storedMetadata.ToFileRef());
        }
        catch
        {
            if (metadataTempPath is not null)
            {
                FileSystemMetadataStore.Discard(metadataTempPath);
            }

            RollbackCommittedObject(targetPath);
            throw;
        }

        return new PutFileResult(storedMetadata.ToFileRef());
    }

    public async Task<OpenReadResult> OpenReadAsync(OpenReadRequest request, CancellationToken cancellationToken = default)
    {
        var key = StorageKey.Normalize(request.Key);
        var targetPath = _paths.GetObjectPath(key);

        if (!File.Exists(targetPath))
        {
            throw new FileNotFoundStorageException(key);
        }

        var storedMetadata = await _metadata.ReadAsync(key, cancellationToken);
        var stream = new FileStream(
            targetPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        return new OpenReadResult
        {
            Content = stream,
            File = storedMetadata?.ToFileRef() ?? CreateFallbackFileRef(key, stream.Length),
        };
    }

    public Task DeleteAsync(DeleteFileRequest request, CancellationToken cancellationToken = default)
    {
        var key = StorageKey.Normalize(request.Key);
        var targetPath = _paths.GetObjectPath(key);
        var metadataPath = _paths.GetMetadataPath(key);
        var metadataExists = File.Exists(metadataPath);
        var objectExists = File.Exists(targetPath);

        if (!metadataExists && !objectExists)
        {
            if (request.IgnoreNotFound)
            {
                return Task.CompletedTask;
            }

            throw new FileNotFoundStorageException(key);
        }

        if (metadataExists)
        {
            File.Delete(metadataPath);
        }

        if (objectExists)
        {
            File.Delete(targetPath);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(ExistsFileRequest request, CancellationToken cancellationToken = default)
    {
        var key = StorageKey.Normalize(request.Key);
        return Task.FromResult(File.Exists(_paths.GetObjectPath(key)));
    }

    public async Task<FileMetadata?> GetMetadataAsync(GetMetadataRequest request, CancellationToken cancellationToken = default)
    {
        var key = StorageKey.Normalize(request.Key);
        var targetPath = _paths.GetObjectPath(key);

        if (!File.Exists(targetPath))
        {
            return null;
        }

        var storedMetadata = await _metadata.ReadAsync(key, cancellationToken);
        if (storedMetadata is null)
        {
            return new FileMetadata
            {
                File = CreateFallbackFileRef(key, new FileInfo(targetPath).Length),
            };
        }

        return new FileMetadata
        {
            File = storedMetadata.ToFileRef(),
            UserMetadata = storedMetadata.UserMetadata,
        };
    }

    private static FileRef CreateFallbackFileRef(string key, long length)
        => new()
        {
            Key = key,
            Length = length,
        };

    private static void RollbackCommittedObject(string targetPath)
    {
        if (File.Exists(targetPath))
        {
            File.Delete(targetPath);
        }
    }
}
