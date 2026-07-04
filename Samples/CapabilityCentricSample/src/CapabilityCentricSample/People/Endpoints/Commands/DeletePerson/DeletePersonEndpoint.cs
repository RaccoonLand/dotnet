using CapabilityCentricSample.People.Domain.Entities;
using CapabilityCentricSample.People.Shared;
using CapabilityCentricSample.Shared.Localizations;
using CapabilityCentricSample.Shared.Persistence.Commands;
using Microsoft.EntityFrameworkCore;
using RaccoonLand.Core.Domain.Exceptions;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;
using RaccoonLand.Core.RequestProcessing.Abstractions.Results;
using RaccoonLand.Modules.MessageLocalization.Abstraction;

namespace CapabilityCentricSample.People.Endpoints.Commands.DeletePerson;

public sealed class DeletePersonEndpoint(CapabilityCentricSampleCommandDbContext _dbContext, IMessageLocalization _messageLocalization)
    : IEndpoint<DeletePersonCommand>
{
    public async Task<Result> ExecuteAsync(DeletePersonCommand request, CancellationToken cancellationToken)
    {
        var person = await _dbContext.Set<Person>()
            .SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (person is null)
            return Result.Failure(new PipelineMessage(SharedBusinessMessageTemplates.ENTITY_NOT_FOUND, 
                _messageLocalization[SharedBusinessMessageTemplates.ENTITY_NOT_FOUND, PersonLocalizations.PERSON]));

        _dbContext.Remove(person);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
