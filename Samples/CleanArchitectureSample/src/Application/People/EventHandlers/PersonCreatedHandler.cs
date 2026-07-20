using CleanArchitectureSample.People.Domain.Events;
using Microsoft.Extensions.Logging;
using RaccoonLand.Modules.Messaging.Abstractions;

namespace CleanArchitectureSample.Application.People.EventHandlers;

/// <summary>
/// Sample in-service handler for <see cref="PersonCreated"/>. Runs on the outbox relay worker,
/// not inside the creating HTTP request.
/// </summary>
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
