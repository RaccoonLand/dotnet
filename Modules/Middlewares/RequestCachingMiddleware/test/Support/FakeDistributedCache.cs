using Microsoft.Extensions.Caching.Distributed;

namespace RaccoonLand.Modules.Middlewares.RequestCachingMiddleware.Tests.Support;

internal sealed class FakeDistributedCache : IDistributedCache
{
    private readonly Dictionary<string, byte[]> _store = new(StringComparer.Ordinal);

    public int GetCount { get; private set; }
    public int SetCount { get; private set; }
    public int RemoveCount { get; private set; }

    public Exception? GetException { get; set; }
    public Exception? SetException { get; set; }
    public Exception? RemoveException { get; set; }

    public List<(string Key, byte[] Value, DistributedCacheEntryOptions Options)> Sets { get; } = [];
    public List<string> RemovedKeys { get; } = [];

    public byte[]? Get(string key) => GetAsync(key).GetAwaiter().GetResult();

    public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        GetCount++;
        token.ThrowIfCancellationRequested();

        if (GetException is not null)
        {
            throw GetException;
        }

        return Task.FromResult(_store.TryGetValue(key, out var value) ? value : null);
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        => SetAsync(key, value, options).GetAwaiter().GetResult();

    public Task SetAsync(
        string key,
        byte[] value,
        DistributedCacheEntryOptions options,
        CancellationToken token = default)
    {
        SetCount++;
        token.ThrowIfCancellationRequested();

        if (SetException is not null)
        {
            throw SetException;
        }

        Sets.Add((key, value, options));
        _store[key] = value;
        return Task.CompletedTask;
    }

    public void Refresh(string key) => RefreshAsync(key).GetAwaiter().GetResult();

    public Task RefreshAsync(string key, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public void Remove(string key) => RemoveAsync(key).GetAwaiter().GetResult();

    public Task RemoveAsync(string key, CancellationToken token = default)
    {
        RemoveCount++;
        token.ThrowIfCancellationRequested();

        if (RemoveException is not null)
        {
            throw RemoveException;
        }

        RemovedKeys.Add(key);
        _store.Remove(key);
        return Task.CompletedTask;
    }

    public void Seed(string key, byte[] value) => _store[key] = value;
}
