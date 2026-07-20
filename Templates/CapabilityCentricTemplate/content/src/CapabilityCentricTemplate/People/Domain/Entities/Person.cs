using CapabilityCentricTemplate.People.Domain.Events;
using CapabilityCentricTemplate.People.Domain.ValueObjects;
using RaccoonLand.Core.Domain.Abstractions;

namespace CapabilityCentricTemplate.People.Domain.Entities;

public sealed class Person : AggregateRoot<int>
{
    public FirstName FirstName { get; private set; } = null!;
    public LastName LastName { get; private set; } = null!;

    private Person()
    {
    }

    public static Person Create(FirstName firstName, LastName lastName)
    {
        var person = new Person
        {
            FirstName = firstName,
            LastName = lastName,
        };

        person.RaiseDomainEvent(new PersonCreated(person.BusinessKey));
        person.RaiseServiceEvent(new PersonCreatedIntegrationEvent(person.BusinessKey));
        return person;
    }
}
