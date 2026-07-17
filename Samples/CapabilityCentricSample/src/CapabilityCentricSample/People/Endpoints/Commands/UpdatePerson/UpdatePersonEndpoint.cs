using CapabilityCentricSample.People.Domain.Entities;
using CapabilityCentricSample.People.Domain.ValueObjects;
using CapabilityCentricSample.People.Shared;
using CapabilityCentricSample.Shared.Localizations;
using CapabilityCentricSample.Shared.Persistence.Commands;
using CapabilityCentricSample.Shared.ValueObjects;
using Microsoft.EntityFrameworkCore;
using RaccoonLand.Core.Domain.Exceptions;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;
using RaccoonLand.Core.RequestProcessing.Abstractions.Results;
using RaccoonLand.Modules.MessageLocalization.Abstraction;

namespace CapabilityCentricSample.People.Endpoints.Commands.UpdatePerson;

public sealed class UpdatePersonEndpoint(CapabilityCentricSampleCommandDbContext dbContext, IMessageLocalization _messageLocalization)
    : IEndpoint<UpdatePersonCommand>
{
    public async Task<Result> ExecuteAsync(UpdatePersonCommand request, CancellationToken cancellationToken)
    {
        var person = await dbContext.Set<Person>()
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

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
