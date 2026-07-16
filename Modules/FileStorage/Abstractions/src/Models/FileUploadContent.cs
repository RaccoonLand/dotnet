namespace RaccoonLand.Modules.FileStorage.Abstractions;

/// <summary>
/// Web-agnostic upload payload: a content stream plus the metadata needed to call
/// <see cref="FileStoragePutHelper"/> / <see cref="IFileStorage.PutAsync"/>.
/// Controllers typically build this from <c>IFormFile</c> via the FileStorage.AspNetCore package.
/// </summary>
public sealed class FileUploadContent : IAsyncDisposable, IDisposable
{
    /// <summary>Readable content stream. The caller owns disposal unless transferred to storage.</summary>
    public required Stream Content { get; init; }

    /// <summary>MIME content type (for example <c>image/jpeg</c> or <c>application/pdf</c>).</summary>
    public required string ContentType { get; init; }

    /// <summary>Declared content length in bytes. Prefer the known length from the upload source.</summary>
    public long ContentLength { get; init; }

    /// <summary>Original file name when available (informational; not used as a storage key).</summary>
    public string? FileName { get; init; }

    /// <inheritdoc />
    public void Dispose() => Content.Dispose();

    /// <inheritdoc />
    public ValueTask DisposeAsync() => Content.DisposeAsync();
}
