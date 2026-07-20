using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RaccoonLand.Modules.Messaging.Abstractions;

namespace RaccoonLand.Modules.Messaging.OutboxRelay;

/// <summary>
/// Background worker that polls <see cref="IOutboxEventStore"/> and:
/// <list type="bullet">
/// <item><description>Dispatches <see cref="OutboxEventCategory.Domain"/> rows via <see cref="IDomainEventDispatcher"/> (same service, outside the originating request).</description></item>
/// <item><description>Publishes <see cref="OutboxEventCategory.Service"/> rows via <see cref="IServiceEventPublisher"/> when enabled.</description></item>
/// </list>
/// Successful handling marks the row processed; failures leave the claim lease to expire for retry (at-least-once).
/// For Service rows, publisher failures (including unroutable RabbitMQ publishes with mandatory confirms)
/// also leave the claim for retry so messages are not silently lost after MarkProcessed.
/// </summary>
public sealed class OutboxRelayBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<OutboxRelayOptions> options,
    ILogger<OutboxRelayBackgroundService> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IOptionsMonitor<OutboxRelayOptions> _options = options;
    private readonly ILogger<OutboxRelayBackgroundService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox relay worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var snapshot = _options.CurrentValue;
            var hadWork = false;

            try
            {
                hadWork = await ProcessOnceAsync(snapshot, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Outbox relay poll cycle failed.");
            }

            var delay = hadWork ? TimeSpan.Zero : snapshot.PollInterval;
            if (delay > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        _logger.LogInformation("Outbox relay worker stopped.");
    }

    private async Task<bool> ProcessOnceAsync(OutboxRelayOptions options, CancellationToken cancellationToken)
    {
        if (!options.ProcessDomainEvents && !options.ProcessServiceEvents)
        {
            _logger.LogWarning(
                "OutboxRelay ProcessDomainEvents and ProcessServiceEvents are both false; nothing to process.");
            return false;
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IOutboxEventStore>();

        string? categoryFilter = null;
        if (options.ProcessDomainEvents && !options.ProcessServiceEvents)
        {
            categoryFilter = OutboxEventCategory.Domain;
        }
        else if (!options.ProcessDomainEvents && options.ProcessServiceEvents)
        {
            categoryFilter = OutboxEventCategory.Service;
        }

        if (options.ClaimLease < TimeSpan.FromSeconds(1))
        {
            throw new InvalidOperationException("OutboxRelayOptions.ClaimLease must be at least one second.");
        }

        var batch = await store.ClaimPendingAsync(
            options.BatchSize,
            categoryFilter,
            options.ClaimLease,
            cancellationToken);
        if (batch.Count == 0)
        {
            return false;
        }

        var dispatcher = options.ProcessDomainEvents
            ? scope.ServiceProvider.GetService<IDomainEventDispatcher>()
            : null;
        var publisher = options.ProcessServiceEvents
            ? scope.ServiceProvider.GetService<IServiceEventPublisher>()
            : null;

        if (options.ProcessDomainEvents && dispatcher is null)
        {
            throw new InvalidOperationException(
                "ProcessDomainEvents is enabled but IDomainEventDispatcher is not registered. " +
                "Call AddRaccoonLandOutboxRelay.");
        }

        if (options.ProcessServiceEvents && publisher is null)
        {
            throw new InvalidOperationException(
                "ProcessServiceEvents is enabled but IServiceEventPublisher is not registered. " +
                "Call AddRaccoonLandRabbitMqServiceEvents (or register another IServiceEventPublisher) " +
                "or disable ProcessServiceEvents.");
        }

        foreach (var item in batch)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (string.Equals(item.Category, OutboxEventCategory.Domain, StringComparison.Ordinal))
                {
                    if (!options.ProcessDomainEvents)
                    {
                        continue;
                    }

                    await dispatcher!.DispatchAsync(item, cancellationToken);
                }
                else if (string.Equals(item.Category, OutboxEventCategory.Service, StringComparison.Ordinal))
                {
                    if (!options.ProcessServiceEvents)
                    {
                        continue;
                    }

                    await publisher!.PublishAsync(item, cancellationToken);
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Outbox event {item.EventId} has unknown Category '{item.Category}'. " +
                        $"Expected '{OutboxEventCategory.Domain}' or '{OutboxEventCategory.Service}'.");
                }

                await store.MarkProcessedAsync([item.ToClaim()], cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed processing outbox event {EventId} ({EventType}, {Category}); will retry after claim lease.",
                    item.EventId,
                    item.EventType,
                    item.Category);
            }
        }

        return true;
    }
}
