using CapabilityCentricSample.People.Domain.Entities;
using CapabilityCentricSample.People.Shared;
using CapabilityCentricSample.Shared.Localizations;
using CapabilityCentricSample.Shared.Persistence.Commands;
using Microsoft.EntityFrameworkCore;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;
using RaccoonLand.Core.RequestProcessing.Abstractions.Results;
using RaccoonLand.Modules.FileStorage.Abstractions;
using RaccoonLand.Modules.MessageLocalization.Abstraction;

namespace CapabilityCentricSample.People.Endpoints.Commands.SetPersonPhoto;

public sealed class SetPersonPhotoEndpoint(
    CapabilityCentricSampleCommandDbContext dbContext,
    IFileStorage fileStorage,
    IMessageLocalization messageLocalization)
    : IEndpoint<SetPersonPhotoCommand>
{
    public async Task<Result> ExecuteAsync(SetPersonPhotoCommand request, CancellationToken cancellationToken)
    {
        var person = await dbContext.Set<Person>()
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (person is null)
            return Result.Failure(new PipelineMessage(
                SharedBusinessMessageTemplates.ENTITY_NOT_FOUND,
                messageLocalization.Get(SharedBusinessMessageTemplates.ENTITY_NOT_FOUND, PersonLocalizations.PERSON)));

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
