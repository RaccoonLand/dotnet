using CapabilityCentricTemplate.People.Persistence.Queries;
using CapabilityCentricTemplate.Shared.Persistence.Queries;
using Microsoft.EntityFrameworkCore;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Results;

namespace CapabilityCentricTemplate.People.Endpoints.Queries.GetPersonById;

public sealed class GetPersonByIdEndpoint(TemplateQueryDbContext dbContext)
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
            FirstName = person.FirstName,
            LastName = person.LastName,
        });
    }
}
