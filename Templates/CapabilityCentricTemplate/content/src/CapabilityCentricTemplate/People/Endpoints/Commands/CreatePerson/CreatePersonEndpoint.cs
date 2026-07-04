using CapabilityCentricTemplate.People.Domain.Entities;
using CapabilityCentricTemplate.People.Domain.ValueObjects;
using CapabilityCentricTemplate.Shared.Persistence.Commands;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Results;

namespace CapabilityCentricTemplate.People.Endpoints.Commands.CreatePerson;

public sealed class CreatePersonEndpoint(TemplateCommandDbContext dbContext)
    : IEndpoint<CreatePersonCommand, int>
{
    public async Task<Result<int>> ExecuteAsync(CreatePersonCommand request, CancellationToken cancellationToken)
    {
        var person = Person.Create(
            FirstName.From(request.FirstName),
            LastName.From(request.LastName));

        dbContext.Set<Person>().Add(person);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(person.Id);
    }
}
