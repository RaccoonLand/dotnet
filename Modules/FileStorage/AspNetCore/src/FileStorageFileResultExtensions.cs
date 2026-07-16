using Microsoft.AspNetCore.Mvc;
using RaccoonLand.Modules.FileStorage.Abstractions;

namespace RaccoonLand.Modules.FileStorage.AspNetCore;

/// <summary>Builds ASP.NET Core file results from <see cref="IFileStorage.OpenReadAsync"/>.</summary>
public static class FileStorageFileResultExtensions
{
    /// <summary>
    /// Opens <paramref name="key"/> for reading and returns a <see cref="FileStreamResult"/>.
    /// Stream ownership transfers to the result (do not dispose the open result yourself).
    /// </summary>
    /// <param name="fileStorage">Active storage provider.</param>
    /// <param name="key">Storage key to open.</param>
    /// <param name="downloadFileName">
    /// When set, sends <c>Content-Disposition: attachment</c> with this file name.
    /// When null, the response is suitable for inline browser playback.
    /// </param>
    /// <param name="enableRangeProcessing">Enables HTTP Range requests (needed for video seeking).</param>
    /// <param name="cancellationToken">Cancellation token for opening the object.</param>
    public static async Task<FileStreamResult> OpenReadFileResultAsync(
        this IFileStorage fileStorage,
        string key,
        string? downloadFileName = null,
        bool enableRangeProcessing = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileStorage);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var open = await fileStorage.OpenReadAsync(
            new OpenReadRequest { Key = key },
            cancellationToken);

        return new FileStreamResult(open.Content, open.File.ContentType ?? "application/octet-stream")
        {
            FileDownloadName = downloadFileName,
            EnableRangeProcessing = enableRangeProcessing,
            LastModified = open.File.CreatedAtUtc,
        };
    }

    /// <summary>
    /// Same as <see cref="OpenReadFileResultAsync(IFileStorage, string, string?, bool, CancellationToken)"/>
    /// but returns <see cref="IActionResult"/> for controller action signatures.
    /// </summary>
    public static async Task<IActionResult> OpenReadActionResultAsync(
        this IFileStorage fileStorage,
        string key,
        string? downloadFileName = null,
        bool enableRangeProcessing = true,
        CancellationToken cancellationToken = default)
        => await fileStorage.OpenReadFileResultAsync(
            key,
            downloadFileName,
            enableRangeProcessing,
            cancellationToken);
}
