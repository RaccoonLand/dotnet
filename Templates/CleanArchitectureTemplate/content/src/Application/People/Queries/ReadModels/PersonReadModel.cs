using RaccoonLand.Modules.Persistence.SqlServer.Queries;

namespace CleanArchitectureTemplate.Application.People.Queries.ReadModels;

public sealed class PersonReadModel : QueryAggregateRoot<int>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}