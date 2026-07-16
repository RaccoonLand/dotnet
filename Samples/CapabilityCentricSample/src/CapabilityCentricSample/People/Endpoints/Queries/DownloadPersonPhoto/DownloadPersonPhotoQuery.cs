using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;

namespace CapabilityCentricSample.People.Endpoints.Queries.DownloadPersonPhoto;

public sealed class DownloadPersonPhotoQuery : IQuery<DownloadPersonFileResult?>
{
    public int Id { get; init; }
}
