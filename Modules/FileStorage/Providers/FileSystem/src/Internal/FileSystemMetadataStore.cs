using System.Text.Json;
using RaccoonLand.Modules.FileStorage.Abstractions;

namespace RaccoonLand.Modules.FileStorage.FileSystem.Internal;

internal sealed class FileSystemMetadataStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly FileSystemPathLayout _paths;

    public FileSystemMetadataStore(FileSystemPathLayout paths) => _paths = paths;

    public async Task<string> WriteToTempAsync(StoredMetadata metadata, CancellationToken cancellationToken)
    {
        var metadataPath = _paths.GetMetadataPath(metadata.Key);
        var tempPath = metadataPath + ".tmp-" + Guid.NewGuid().ToString("N");
        EnsureDirectory(tempPath);

        var succeeded = false;

        try
        {
            await using (var stream = new FileStream(
                             tempPath,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             bufferSize: 4096,
                             FileOptions.Asynchronous))
            {
                await JsonSerializer.SerializeAsync(stream, metadata, JsonOptions, cancellationToken);
            }

            succeeded = true;
            return tempPath;
        }
        finally
        {
            if (!succeeded)
            {
                Discard(tempPath);
            }
        }
    }

    public static void Commit(string tempPath, string metadataPath)
        => File.Move(tempPath, metadataPath, overwrite: true);

    public static void Discard(string tempPath) => FileSystemAtomicFileWriter.Discard(tempPath);

    public async Task<StoredMetadata?> ReadAsync(string key, CancellationToken cancellationToken = default)
    {
        var metadataPath = _paths.GetMetadataPath(key);

        if (!File.Exists(metadataPath))
        {
            return null;
        }

        try
        {
            await using var stream = new FileStream(
                metadataPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            return await JsonSerializer.DeserializeAsync<StoredMetadata>(stream, JsonOptions, cancellationToken);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public void Delete(string key)
    {
        var metadataPath = _paths.GetMetadataPath(key);

        if (File.Exists(metadataPath))
        {
            File.Delete(metadataPath);
        }
    }

    public async Task<DateTimeOffset> ResolveCreatedAtUtcAsync(
        string key,
        PutMode mode,
        CancellationToken cancellationToken = default)
    {
        if (mode is PutMode.CreateOnly)
        {
            return DateTimeOffset.UtcNow;
        }

        return (await ReadAsync(key, cancellationToken))?.CreatedAtUtc ?? DateTimeOffset.UtcNow;
    }

    private static void EnsureDirectory(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath)!;
        Directory.CreateDirectory(directory);
    }
}
