namespace RaccoonLand.Modules.FileStorage.Abstractions;

/// <summary>Upload part result.</summary>
public sealed record UploadPartResult(int PartNumber, string ETag);
