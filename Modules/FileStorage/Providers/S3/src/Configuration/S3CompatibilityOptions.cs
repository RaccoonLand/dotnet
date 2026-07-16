namespace RaccoonLand.Modules.FileStorage.S3.Configuration;

public sealed class S3CompatibilityOptions
{
    public bool? ForcePathStyle { get; set; }

    public bool? DisableChunkedEncoding { get; set; }

    public bool? DisablePayloadSigning { get; set; }

    public TimeSpan? RequestTimeout { get; set; }
}
