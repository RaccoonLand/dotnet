using RaccoonLand.Modules.Persistence.SqlServer.Queries;

namespace CapabilityCentricTemplate.People.Persistence.Queries;

public sealed class Person : QueryAggregateRoot<int>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}
