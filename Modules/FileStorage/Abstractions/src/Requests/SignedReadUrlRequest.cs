namespace RaccoonLand.Modules.FileStorage.Abstractions;

/// <summary>Signed read URL request.</summary>
public sealed class SignedReadUrlRequest
{
    public required string Key { get; init; }

    public TimeSpan? Expiry { get; init; }

    public string? ContentType { get; init; }
}
