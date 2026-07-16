namespace RaccoonLand.Modules.FileStorage.Abstractions;

/// <summary>Multipart upload session.</summary>
public sealed record MultipartUploadSession(string Key, string UploadId);
