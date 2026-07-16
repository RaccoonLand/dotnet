using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;

namespace CapabilityCentricSample.People.Endpoints.Queries.DownloadPersonResume;

public sealed class DownloadPersonResumeQuery : IQuery<DownloadPersonFileResult?>
{
    public int Id { get; init; }
}
