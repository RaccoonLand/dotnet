using CapabilityCentricSample.People.Persistence.Queries;
using CapabilityCentricSample.Shared.Persistence.Queries;
using Microsoft.EntityFrameworkCore;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Results;

namespace CapabilityCentricSample.People.Endpoints.Queries.GetPersonById;

public sealed class GetPersonByIdEndpoint(CapabilityCentricSampleQueryDbContext dbContext)
    : IEndpoint<GetPersonByIdQuery, GetPersonByIdResponse?>
{
    public async Task<Result<GetPersonByIdResponse?>> ExecuteAsync(
        GetPersonByIdQuery request,
        CancellationToken cancellationToken)
    {
        var person = await dbContext.Set<Person>()
            .SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (person is null)
            return Result<GetPersonByIdResponse?>.Success(null);

        return Result<GetPersonByIdResponse?>.Success(new GetPersonByIdResponse
        {
            Id = person.Id,
            EmployeeCode = person.EmployeeCode,
            FirstName = person.FirstName,
            LastName = person.LastName,
            NationalCode = person.NationalCode,
            Email = person.Email,
            MobileNumber = person.MobileNumber,
            EmploymentDate = person.EmploymentDate,
            Status = person.Status,
            CurrentDepartmentId = person.CurrentDepartmentId,
            DepartmentAssignedAt = person.DepartmentAssignedAt,
            PhotoFileKey = person.PhotoFileKey,
            ResumeFileKey = person.ResumeFileKey,
        });
    }
}
