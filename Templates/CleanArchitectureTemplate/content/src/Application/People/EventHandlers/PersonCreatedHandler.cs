using CleanArchitectureTemplate.People.Domain.Events;
using Microsoft.Extensions.Logging;
using RaccoonLand.Modules.Messaging.Abstractions;

namespace CleanArchitectureTemplate.Application.People.EventHandlers;

public sealed class PersonCreatedHandler(ILogger<PersonCreatedHandler> logger) : IDomainEventHandler<PersonCreated>
{
    public Task HandleAsync(
        PersonCreated domainEvent,
        DomainEventHandlingContext context,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Handled domain event {EventType} for person {PersonBusinessKey} (EventId={EventId}).",
            domainEvent.EventType,
            domainEvent.PersonBusinessKey,
            domainEvent.EventId);

        return Task.CompletedTask;
    }
}
