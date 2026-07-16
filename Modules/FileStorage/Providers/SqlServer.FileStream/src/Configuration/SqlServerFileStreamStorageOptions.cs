namespace RaccoonLand.Modules.FileStorage.SqlServer.FileStream.Configuration;

public sealed class SqlServerFileStreamStorageOptions
{
    public const string SectionName = "FileStorage:SqlServer:FileStream";

    public required string ConnectionString { get; set; }

    public string SchemaName { get; set; } = "dbo";

    public string TableName { get; set; } = "FileBlobStreams";

    public string ContentColumnName { get; set; } = "Content";

    public string FileGroupName { get; set; } = "FileStreamGroup";

    /// <summary>
    /// When true, creates the FILESTREAM table if it does not exist. The FILESTREAM filegroup must already exist.
    /// </summary>
    public bool EnsureSchema { get; set; }
}
