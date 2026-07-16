namespace RaccoonLand.Modules.FileStorage.FileSystem.Configuration;

public sealed class FileSystemStorageOptions
{
    public const string SectionName = "FileStorage:FileSystem";

    public required string RootPath { get; set; }
}
