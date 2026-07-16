namespace CleanArchitectureSample.Application.People.Queries;

public sealed class DownloadPersonFileResult
{
    public required Stream Content { get; init; }

    public string? ContentType { get; init; }

    public string FileName { get; init; } = "file";

    public long? ContentLength { get; init; }
}
