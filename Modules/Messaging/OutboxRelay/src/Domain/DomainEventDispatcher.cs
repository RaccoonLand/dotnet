using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RaccoonLand.Core.Domain.Events;
using RaccoonLand.Modules.Messaging.Abstractions;

namespace RaccoonLand.Modules.Messaging.OutboxRelay;

/// <summary>
/// Default <see cref="IDomainEventDispatcher"/>: deserializes the outbox payload to the registered CLR
/// type and invokes all DI-registered <see cref="IDomainEventHandler{TEvent}"/> instances for that type.
/// </summary>
public sealed class DomainEventDispatcher(
    IServiceScopeFactory scopeFactory,
    DomainEventHandlerRegistry registry,
    ILogger<DomainEventDispatcher> logger) : IDomainEventDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly DomainEventHandlerRegistry _registry = registry;
    private readonly ILogger<DomainEventDispatcher> _logger = logger;

    public async Task DispatchAsync(OutboxEventRecord outboxEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(outboxEvent);

        if (!_registry.TryGet(outboxEvent.EventType, out var registration))
        {
            throw new InvalidOperationException(
                $"No domain event handler registration for EventType '{outboxEvent.EventType}' (EventId={outboxEvent.EventId}).");
        }

        var domainEvent = JsonSerializer.Deserialize(outboxEvent.Payload, registration.EventClrType, JsonOptions)
            as DomainEvent
            ?? throw new InvalidOperationException(
                $"Failed to deserialize outbox event EventId={outboxEvent.EventId} as {registration.EventClrType.FullName}.");

        if (domainEvent.EventId != outboxEvent.EventId)
        {
            throw new InvalidOperationException(
                $"Domain event payload EventId {domainEvent.EventId} does not match outbox EventId {outboxEvent.EventId}.");
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var handlers = scope.ServiceProvider.GetServices(registration.HandlerServiceType).ToArray();
        if (handlers.Length == 0)
        {
            throw new InvalidOperationException(
                $"EventType '{outboxEvent.EventType}' is registered but no {registration.HandlerServiceType.FullName} is available in DI.");
        }

        var context = new DomainEventHandlingContext { OutboxEvent = outboxEvent };
        var handleMethod = registration.HandlerServiceType.GetMethod(
            nameof(IDomainEventHandler<DomainEvent>.HandleAsync),
            [registration.EventClrType, typeof(DomainEventHandlingContext), typeof(CancellationToken)])
            ?? throw new InvalidOperationException(
                $"Could not locate HandleAsync on {registration.HandlerServiceType.FullName}.");

        foreach (var handler in handlers)
        {
            _logger.LogDebug(
                "Dispatching domain event {EventType} ({EventId}) to {Handler}.",
                outboxEvent.EventType,
                outboxEvent.EventId,
                handler!.GetType().FullName);

            var task = (Task?)handleMethod.Invoke(
                handler,
                [domainEvent, context, cancellationToken])
                ?? throw new InvalidOperationException("HandleAsync returned null Task.");

            await task;
        }
    }
}
