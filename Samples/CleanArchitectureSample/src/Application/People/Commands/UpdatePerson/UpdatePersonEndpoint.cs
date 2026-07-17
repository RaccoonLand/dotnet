using CleanArchitectureSample.Application.Abstractions.Persistence;
using CleanArchitectureSample.People.Domain.Entities;
using CleanArchitectureSample.People.Domain.ValueObjects;
using CleanArchitectureSample.People.Shared;
using CleanArchitectureSample.Shared.Localizations;
using CleanArchitectureSample.Shared.ValueObjects;
using Microsoft.EntityFrameworkCore;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;
using RaccoonLand.Core.RequestProcessing.Abstractions.Results;
using RaccoonLand.Modules.MessageLocalization.Abstraction;

namespace CleanArchitectureSample.Application.People.Commands.UpdatePerson;

public sealed class UpdatePersonEndpoint(ICommandDbContext db, IMessageLocalization _messageLocalization)
    : IEndpoint<UpdatePersonCommand>
{
    public async Task<Result> ExecuteAsync(UpdatePersonCommand request, CancellationToken cancellationToken)
    {
        var person = await db.Set<Person>()
            .SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (person is null)
            return Result.Failure(new PipelineMessage(SharedBusinessMessageTemplates.ENTITY_NOT_FOUND,
                _messageLocalization.Get(SharedBusinessMessageTemplates.ENTITY_NOT_FOUND, PersonLocalizations.PERSON)));
        //throw new DomainException(SharedBusinessMessageTemplates.ENTITY_NOT_FOUND,
        //    SharedBusinessMessageTemplates.ENTITY_NOT_FOUND, PersonLocalizations.PERSON);


        person.Update(
            FirstName.From(request.FirstName),
            LastName.From(request.LastName),
            Email.From(request.Email),
            MobileNumber.From(request.MobileNumber),
            request.Status);

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
