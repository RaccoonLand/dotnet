using CleanArchitectureSample.Application.Abstractions.Persistence;
using CleanArchitectureSample.People.Domain.Entities;
using CleanArchitectureSample.People.Shared;
using CleanArchitectureSample.Shared.Localizations;
using Microsoft.EntityFrameworkCore;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;
using RaccoonLand.Core.RequestProcessing.Abstractions.Results;
using RaccoonLand.Modules.FileStorage.Abstractions;
using RaccoonLand.Modules.MessageLocalization.Abstraction;

namespace CleanArchitectureSample.Application.People.Commands.SetPersonPhoto;

public sealed class SetPersonPhotoEndpoint(
    ICommandDbContext db,
    IFileStorage fileStorage,
    IMessageLocalization messageLocalization)
    : IEndpoint<SetPersonPhotoCommand>
{
    public async Task<Result> ExecuteAsync(SetPersonPhotoCommand request, CancellationToken cancellationToken)
    {
        var person = await db.Set<Person>()
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (person is null)
            return Result.Failure(new PipelineMessage(
                SharedBusinessMessageTemplates.ENTITY_NOT_FOUND,
                messageLocalization[SharedBusinessMessageTemplates.ENTITY_NOT_FOUND, PersonLocalizations.PERSON]));

        await fileStorage.PutAsync(
            FileStoragePutHelper.CreateRequest(
                request.Content,
                request.ContentType,
                FilePutConstraints.Images,
                PutMode.Upsert,
                key: PersonFileKeys.Photo(person.BusinessKey),
                contentLength: request.ContentLength,
                metadata: PersonFileKeys.CreateMetadata(person.BusinessKey)),
            cancellationToken);

        return Result.Success();
    }
}
