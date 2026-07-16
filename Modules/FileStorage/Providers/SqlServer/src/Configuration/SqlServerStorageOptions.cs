namespace RaccoonLand.Modules.FileStorage.SqlServer.Configuration;

public sealed class SqlServerStorageOptions
{
    public const string SectionName = "FileStorage:SqlServer";

    public required string ConnectionString { get; set; }

    public string SchemaName { get; set; } = "dbo";

    public string TableName { get; set; } = "FileBlobs";

    /// <summary>When true, creates the storage table on first use.</summary>
    public bool EnsureSchema { get; set; } = true;
}
