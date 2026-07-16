namespace RaccoonLand.Modules.FileStorage.Abstractions;

/// <summary>Readable content returned by <see cref="IFileStorage.OpenReadAsync"/>.</summary>
public sealed class OpenReadResult : IAsyncDisposable
{
    public required Stream Content { get; init; }

    public required FileRef File { get; init; }

    public async ValueTask DisposeAsync() => await Content.DisposeAsync();
}
