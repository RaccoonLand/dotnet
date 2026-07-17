using CleanArchitectureSample.Application.Abstractions.Persistence;
using CleanArchitectureSample.Application.People.Queries.ReadModels;
using CleanArchitectureSample.People.Shared;
using CleanArchitectureSample.Shared.Localizations;
using Microsoft.EntityFrameworkCore;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;
using RaccoonLand.Core.RequestProcessing.Abstractions.Results;
using RaccoonLand.Modules.FileStorage.Abstractions;
using RaccoonLand.Modules.MessageLocalization.Abstraction;

namespace CleanArchitectureSample.Application.People.Queries.DownloadPersonPhoto;

public sealed class DownloadPersonPhotoEndpoint(
    IQueryDbContext db,
    IFileStorage fileStorage,
    IMessageLocalization messageLocalization)
    : IEndpoint<DownloadPersonPhotoQuery, DownloadPersonFileResult?>
{
    public async Task<Result<DownloadPersonFileResult?>> ExecuteAsync(
        DownloadPersonPhotoQuery request,
        CancellationToken cancellationToken)
    {
        var person = await db.Set<PersonReadModel>()
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (person is null)
            return Result<DownloadPersonFileResult?>.Failure(new PipelineMessage(
                SharedBusinessMessageTemplates.ENTITY_NOT_FOUND,
                messageLocalization.Get(SharedBusinessMessageTemplates.ENTITY_NOT_FOUND, PersonLocalizations.PERSON)));

        if (string.IsNullOrWhiteSpace(person.PhotoFileKey))
            return Result<DownloadPersonFileResult?>.Success(null);

        var open = await fileStorage.OpenReadAsync(
            new OpenReadRequest { Key = person.PhotoFileKey },
            cancellationToken);

        return Result<DownloadPersonFileResult?>.Success(new DownloadPersonFileResult
        {
            Content = open.Content,
            ContentType = open.File.ContentType,
            FileName = "photo",
            ContentLength = open.File.Length,
        });
    }
}
