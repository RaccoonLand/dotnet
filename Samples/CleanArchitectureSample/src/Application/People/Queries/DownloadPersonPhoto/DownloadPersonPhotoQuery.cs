using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;

namespace CleanArchitectureSample.Application.People.Queries.DownloadPersonPhoto;

public sealed class DownloadPersonPhotoQuery : IQuery<DownloadPersonFileResult?>
{
    public int Id { get; init; }
}
