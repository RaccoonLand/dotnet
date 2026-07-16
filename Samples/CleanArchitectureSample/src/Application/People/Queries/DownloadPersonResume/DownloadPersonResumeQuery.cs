using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;

namespace CleanArchitectureSample.Application.People.Queries.DownloadPersonResume;

public sealed class DownloadPersonResumeQuery : IQuery<DownloadPersonFileResult?>
{
    public int Id { get; init; }
}
