namespace RaccoonLand.Modules.FileStorage.Abstractions;

/// <summary>Signed URL result.</summary>
public sealed record SignedUrlResult(Uri Url, DateTimeOffset ExpiresAtUtc, FileRef File);
