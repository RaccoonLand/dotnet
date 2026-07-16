namespace RaccoonLand.Modules.FileStorage.Abstractions;

/// <summary>Delete request.</summary>
public sealed class DeleteFileRequest
{
    public required string Key { get; init; }

    /// <summary>When true, deleting a missing object succeeds.</summary>
    public bool IgnoreNotFound { get; init; } = true;
}
