using CleanArchitectureSample.Application.Abstractions.Persistence;
using CleanArchitectureSample.People.Domain.Entities;
using CleanArchitectureSample.People.Domain.ValueObjects;
using CleanArchitectureSample.Shared.ValueObjects;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Results;

namespace CleanArchitectureSample.Application.People.Commands.CreatePerson;

public sealed class CreatePersonEndpoint(ICommandDbContext db)
    : IEndpoint<CreatePersonCommand, int>
{
    public async Task<Result<int>> ExecuteAsync(CreatePersonCommand request, CancellationToken cancellationToken)
    {
        var person = Person.Create(
            EmployeeCode.From(request.EmployeeCode),
            FirstName.From(request.FirstName),
            LastName.From(request.LastName),
            NationalCode.From(request.NationalCode),
            Email.From(request.Email),
            MobileNumber.From(request.MobileNumber),
            request.EmploymentDate);

        db.Set<Person>().Add(person);
        await db.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(person.Id);
    }
}
