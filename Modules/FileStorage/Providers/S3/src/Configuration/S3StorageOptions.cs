namespace RaccoonLand.Modules.FileStorage.S3.Configuration;

public sealed class S3StorageOptions
{
    public const string SectionName = "FileStorage:S3";

    public required string Bucket { get; set; }

    public string? Region { get; set; }

    public string? Endpoint { get; set; }

    public S3Flavor Flavor { get; set; } = S3Flavor.Aws;

    public string? KeyPrefix { get; set; }

    public S3CredentialsOptions Credentials { get; set; } = new();

    public S3CompatibilityOptions? Compatibility { get; set; }
}
