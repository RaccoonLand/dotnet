using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Modules.Middlewares.RequestCaching.Abstraction;

namespace CapabilityCentricTemplate.People.Endpoints.Queries.GetPersonById;

public sealed class GetPersonByIdQuery : IQuery<GetPersonByIdResponse>, ICacheableRequest
{
    public int Id { get; init; }
    public string GetCacheKey() => $"{GetType().Name}:{Id}";
}

public sealed class GetPersonByIdResponse
{
    public int Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
}
