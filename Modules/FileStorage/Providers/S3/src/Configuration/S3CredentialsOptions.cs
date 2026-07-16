namespace RaccoonLand.Modules.FileStorage.S3.Configuration;

public sealed class S3CredentialsOptions
{
    public string? AccessKeyId { get; set; }

    public string? SecretAccessKey { get; set; }

    public string? SessionToken { get; set; }
}
