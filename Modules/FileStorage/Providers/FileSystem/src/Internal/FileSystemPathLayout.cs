namespace RaccoonLand.Modules.FileStorage.FileSystem.Internal;

internal sealed class FileSystemPathLayout
{
    private readonly string _rootPath;

    public FileSystemPathLayout(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        _rootPath = rootPath;
    }

    public string GetObjectPath(string key)
    {
        var shard = GetShard(key);
        return Path.Combine(_rootPath, shard, key);
    }

    public string GetMetadataPath(string key)
    {
        var shard = GetShard(key);
        return Path.Combine(_rootPath, shard, key + ".meta.json");
    }

    private static string GetShard(string key)
        => key.Length >= 4 ? key[..4] : key;
}
