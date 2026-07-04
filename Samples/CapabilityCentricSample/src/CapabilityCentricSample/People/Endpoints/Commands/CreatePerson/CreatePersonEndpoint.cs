using CapabilityCentricSample.People.Domain.Entities;
using CapabilityCentricSample.People.Domain.ValueObjects;
using CapabilityCentricSample.Shared.Persistence.Commands;
using CapabilityCentricSample.Shared.ValueObjects;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Results;

namespace CapabilityCentricSample.People.Endpoints.Commands.CreatePerson;

public sealed class CreatePersonEndpoint(CapabilityCentricSampleCommandDbContext dbContext)
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

        dbContext.Set<Person>().Add(person);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(person.Id);
    }
}
