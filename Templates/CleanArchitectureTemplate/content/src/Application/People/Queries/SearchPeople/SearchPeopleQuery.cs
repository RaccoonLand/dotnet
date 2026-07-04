using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;

namespace CleanArchitectureTemplate.Application.People.Queries.SearchPeople;

public sealed class SearchPeopleQuery : PagedQuery<SearchPeopleResponse>
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
}

public sealed record SearchPeopleResponse : PageResponse<SearchPeopleItem>;

public sealed class SearchPeopleItem
{
    public int Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
}
