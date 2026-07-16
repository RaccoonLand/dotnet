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

namespace CleanArchitectureSample.Application.People.Commands.DeletePerson;

public sealed class DeletePersonEndpoint(
    ICommandDbContext db,
    IFileStorage fileStorage,
    IMessageLocalization messageLocalization)
    : IEndpoint<DeletePersonCommand>
{
    public async Task<Result> ExecuteAsync(DeletePersonCommand request, CancellationToken cancellationToken)
    {
        var person = await db.Set<Person>()
            .SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (person is null)
            return Result.Failure(new PipelineMessage(
                SharedBusinessMessageTemplates.ENTITY_NOT_FOUND,
                messageLocalization[SharedBusinessMessageTemplates.ENTITY_NOT_FOUND, PersonLocalizations.PERSON]));

        var photoKey = person.PhotoFileKey;
        var resumeKey = person.ResumeFileKey;

        db.Set<Person>().Remove(person);
        await db.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(photoKey))
        {
            await fileStorage.DeleteAsync(
                new DeleteFileRequest { Key = photoKey, IgnoreNotFound = true },
                cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(resumeKey))
        {
            await fileStorage.DeleteAsync(
                new DeleteFileRequest { Key = resumeKey, IgnoreNotFound = true },
                cancellationToken);
        }

        return Result.Success();
    }
}
