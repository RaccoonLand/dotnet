namespace RaccoonLand.Modules.FileStorage.FileSystem.Internal;

using RaccoonLand.Modules.FileStorage.Abstractions;

internal static class FileSystemAtomicFileWriter
{
    internal sealed record TempWriteResult(string TempPath, long Length, string ChecksumSha256);

    public static async Task<TempWriteResult> WriteToTempAsync(
        Stream content,
        string targetPath,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(targetPath)!;
        Directory.CreateDirectory(directory);

        var tempPath = targetPath + ".tmp-" + Guid.NewGuid().ToString("N");
        var succeeded = false;

        try
        {
            await using (var targetStream = new FileStream(
                             tempPath,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             bufferSize: 81920,
                             FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await using var hashingStream = new HashingWriteStream(targetStream);
                await content.CopyToAsync(hashingStream, cancellationToken);
                await hashingStream.FlushAsync(cancellationToken);

                var result = new TempWriteResult(
                    tempPath,
                    hashingStream.BytesWritten,
                    hashingStream.GetChecksumHex());

                succeeded = true;
                return result;
            }
        }
        finally
        {
            if (!succeeded)
            {
                Discard(tempPath);
            }
        }
    }

    public static void Commit(string tempPath, string targetPath, PutMode mode)
    {
        var overwrite = mode is not PutMode.CreateOnly;

        try
        {
            File.Move(tempPath, targetPath, overwrite: overwrite);
        }
        catch (IOException) when (!overwrite)
        {
            throw new FileAlreadyExistsStorageException(ExtractKeyFromObjectPath(targetPath));
        }
    }

    public static void Discard(string tempPath)
    {
        if (File.Exists(tempPath))
        {
            File.Delete(tempPath);
        }
    }

    private static string ExtractKeyFromObjectPath(string targetPath)
        => Path.GetFileName(targetPath);
}
