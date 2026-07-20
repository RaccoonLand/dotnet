using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RaccoonLand.Core.Domain.Events;
using RaccoonLand.Modules.Messaging.Abstractions;

namespace RaccoonLand.Modules.Messaging.RabbitMQ;

/// <summary>
/// Default <see cref="IServiceEventDispatcher"/>: deserializes the payload and invokes registered handlers.
/// </summary>
public sealed class ServiceEventDispatcher(
    IServiceScopeFactory scopeFactory,
    ServiceEventHandlerRegistry registry,
    ILogger<ServiceEventDispatcher> logger) : IServiceEventDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ServiceEventHandlerRegistry _registry = registry;
    private readonly ILogger<ServiceEventDispatcher> _logger = logger;

    public async Task DispatchAsync(ServiceEventMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (!_registry.TryGet(message.EventType, out var registration))
        {
            throw new InvalidOperationException(
                $"No service event handler registration for EventType '{message.EventType}' (EventId={message.EventId}).");
        }

        var serviceEvent = JsonSerializer.Deserialize(message.Payload, registration.EventClrType, JsonOptions)
            as ServiceEvent
            ?? throw new InvalidOperationException(
                $"Failed to deserialize service event EventId={message.EventId} as {registration.EventClrType.FullName}.");

        if (serviceEvent.EventId != message.EventId)
        {
            throw new InvalidOperationException(
                $"Service event payload EventId {serviceEvent.EventId} does not match envelope EventId {message.EventId}.");
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var handlers = scope.ServiceProvider.GetServices(registration.HandlerServiceType).ToArray();
        if (handlers.Length == 0)
        {
            throw new InvalidOperationException(
                $"EventType '{message.EventType}' is registered but no {registration.HandlerServiceType.FullName} is available in DI.");
        }

        var context = new ServiceEventHandlingContext { Message = message };
        var handleMethod = registration.HandlerServiceType.GetMethod(
            nameof(IServiceEventHandler<ServiceEvent>.HandleAsync),
            [registration.EventClrType, typeof(ServiceEventHandlingContext), typeof(CancellationToken)])
            ?? throw new InvalidOperationException(
                $"Could not locate HandleAsync on {registration.HandlerServiceType.FullName}.");

        foreach (var handler in handlers)
        {
            _logger.LogDebug(
                "Dispatching service event {EventType} ({EventId}) to {Handler}.",
                message.EventType,
                message.EventId,
                handler!.GetType().FullName);

            var task = (Task?)handleMethod.Invoke(handler, [serviceEvent, context, cancellationToken])
                ?? throw new InvalidOperationException("HandleAsync returned null Task.");

            await task;
        }
    }
}
