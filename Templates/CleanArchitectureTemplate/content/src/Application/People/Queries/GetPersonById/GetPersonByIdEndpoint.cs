using CleanArchitectureTemplate.Application.Abstractions.Persistence;
using CleanArchitectureTemplate.Application.People.Queries.ReadModels;
using Microsoft.EntityFrameworkCore;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Results;

namespace CleanArchitectureTemplate.Application.People.Queries.GetPersonById;

public sealed class GetPersonByIdEndpoint(IQueryDbContext db)
    : IEndpoint<GetPersonByIdQuery, GetPersonByIdResponse?>
{
    public async Task<Result<GetPersonByIdResponse?>> ExecuteAsync(
        GetPersonByIdQuery request,
        CancellationToken cancellationToken)
    {
        var person = await db.Set<PersonReadModel>()
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