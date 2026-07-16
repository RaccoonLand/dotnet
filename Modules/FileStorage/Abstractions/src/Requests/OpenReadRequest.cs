namespace RaccoonLand.Modules.FileStorage.Abstractions;

/// <summary>Open read request.</summary>
public sealed class OpenReadRequest
{
    public required string Key { get; init; }
}
