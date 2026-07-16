using Microsoft.AspNetCore.Http;
using RaccoonLand.Modules.FileStorage.Abstractions;

namespace RaccoonLand.Modules.FileStorage.AspNetCore;

/// <summary>Maps ASP.NET Core multipart files to <see cref="FileUploadContent"/>.</summary>
public static class FormFileUploadExtensions
{
    /// <summary>
    /// Opens the form file stream and returns a <see cref="FileUploadContent"/>.
    /// The caller must dispose the returned instance (typically with <c>await using</c>).
    /// </summary>
    /// <exception cref="FileStorageValidationException">When <paramref name="file"/> is missing or empty.</exception>
    public static FileUploadContent ToFileUploadContent(this IFormFile? file)
    {
        if (file is null || file.Length <= 0)
        {
            throw new FileStorageValidationException("A non-empty uploaded file is required.");
        }

        var contentType = string.IsNullOrWhiteSpace(file.ContentType)
            ? "application/octet-stream"
            : file.ContentType.Trim();

        return new FileUploadContent
        {
            Content = file.OpenReadStream(),
            ContentType = contentType,
            ContentLength = file.Length,
            FileName = string.IsNullOrWhiteSpace(file.FileName) ? null : file.FileName,
        };
    }
}
