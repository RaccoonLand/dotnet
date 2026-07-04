using CleanArchitectureTemplate.Application.Abstractions.Persistence;
using CleanArchitectureTemplate.People.Domain.Entities;
using CleanArchitectureTemplate.People.Domain.ValueObjects;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Results;

namespace CleanArchitectureTemplate.Application.People.Commands.CreatePerson;

public sealed class CreatePersonEndpoint(ICommandDbContext db)
    : IEndpoint<CreatePersonCommand, int>
{
    public async Task<Result<int>> ExecuteAsync(CreatePersonCommand request, CancellationToken cancellationToken)
    {
        var person = Person.Create(
            FirstName.From(request.FirstName),
            LastName.From(request.LastName));

        db.Set<Person>().Add(person);
        await db.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(person.Id);
    }
}