using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RaccoonLand.Modules.Messaging.Abstractions;

namespace RaccoonLand.Modules.Messaging.RabbitMQ;

/// <summary>
/// Consumes service events from RabbitMQ, gates duplicates via <see cref="IInboxStore"/>, and dispatches
/// to <see cref="IServiceEventDispatcher"/>. Failed deliveries retry up to
/// <see cref="RabbitMqServiceEventConsumerOptions.MaxDeliveryAttempts"/> then go to a dead-letter queue.
/// </summary>
public sealed class RabbitMqServiceEventConsumerBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<RabbitMqServiceEventConsumerOptions> _options;
    private readonly ILogger<RabbitMqServiceEventConsumerBackgroundService> _logger;

    public RabbitMqServiceEventConsumerBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<RabbitMqServiceEventConsumerOptions> options,
        ILogger<RabbitMqServiceEventConsumerBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RabbitMQ service-event consumer starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunConsumerLoopAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "RabbitMQ service-event consumer loop failed; retrying in 5 seconds.");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        _logger.LogInformation("RabbitMQ service-event consumer stopped.");
    }

    private async Task RunConsumerLoopAsync(CancellationToken stoppingToken)
    {
        var snapshot = _options.CurrentValue;
        ValidateRuntimeOptions(snapshot);

        var factory = CreateConnectionFactory(snapshot);
        await using var connection = await factory.CreateConnectionAsync(stoppingToken);

        // Publisher confirms on the same channel so retry/poison publishes are confirmed before ACK.
        var channelOptions = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);
        await using var channel = await connection.CreateChannelAsync(channelOptions, stoppingToken);

        await channel.BasicQosAsync(0, snapshot.PrefetchCount, false, stoppingToken);

        if (snapshot.DeclareTopology)
        {
            await DeclareTopologyAsync(channel, snapshot, stoppingToken);
        }

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, args) =>
        {
            await HandleDeliveryAsync(channel, args, stoppingToken);
        };

        await channel.BasicConsumeAsync(
            queue: snapshot.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation(
            "Consuming queue {Queue} bound to exchange {Exchange}.",
            snapshot.QueueName,
            snapshot.ExchangeName);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private static async Task DeclareTopologyAsync(
        IChannel channel,
        RabbitMqServiceEventConsumerOptions snapshot,
        CancellationToken stoppingToken)
    {
        await channel.ExchangeDeclareAsync(
            snapshot.ExchangeName,
            snapshot.ExchangeType,
            durable: snapshot.DurableExchange,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        Dictionary<string, object?>? queueArguments = null;
        if (snapshot.EnableDeadLetterTopology)
        {
            var dlx = snapshot.ResolveDeadLetterExchangeName();
            var dlq = snapshot.ResolveDeadLetterQueueName();

            await channel.ExchangeDeclareAsync(
                dlx,
                ExchangeType.Fanout,
                durable: true,
                autoDelete: false,
                arguments: null,
                cancellationToken: stoppingToken);

            await channel.QueueDeclareAsync(
                dlq,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: stoppingToken);

            await channel.QueueBindAsync(
                dlq,
                dlx,
                routingKey: string.Empty,
                arguments: null,
                cancellationToken: stoppingToken);

            queueArguments = new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = dlx,
            };
        }

        await channel.QueueDeclareAsync(
            snapshot.QueueName,
            durable: snapshot.DurableQueue,
            exclusive: false,
            autoDelete: false,
            arguments: queueArguments,
            cancellationToken: stoppingToken);

        foreach (var bindingKey in snapshot.BindingKeys.Distinct(StringComparer.Ordinal))
        {
            await channel.QueueBindAsync(
                snapshot.QueueName,
                snapshot.ExchangeName,
                bindingKey,
                arguments: null,
                cancellationToken: stoppingToken);
        }
    }

    private async Task HandleDeliveryAsync(
        IChannel channel,
        BasicDeliverEventArgs args,
        CancellationToken stoppingToken)
    {
        ServiceEventMessage? message = null;

        try
        {
            message = ServiceEventMessageMapper.FromDelivery(args);
            var snapshot = _options.CurrentValue;

            await using var scope = _scopeFactory.CreateAsyncScope();
            var inbox = scope.ServiceProvider.GetRequiredService<IInboxStore>();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IServiceEventDispatcher>();

            var claimAttempt = await inbox.TryClaimAsync(
                message.EventId,
                message.EventType,
                snapshot.InboxClaimLease,
                stoppingToken);

            switch (claimAttempt.Result)
            {
                case InboxClaimResult.AlreadyProcessed:
                    await channel.BasicAckAsync(args.DeliveryTag, multiple: false, stoppingToken);
                    _logger.LogDebug(
                        "Skipped already-processed service event {EventId} ({EventType}).",
                        message.EventId,
                        message.EventType);
                    return;

                case InboxClaimResult.ClaimHeldByOther:
                    // Contention is outside MaxDeliveryAttempts (see poison docs). Back off before
                    // requeue to avoid a hot loop against a live inbox claim.
                    var delay = snapshot.ClaimHeldByOtherRequeueDelay;
                    if (delay > TimeSpan.Zero)
                    {
                        _logger.LogDebug(
                            "Inbox claim held for service event {EventId}; delaying {Delay} before requeue.",
                            message.EventId,
                            delay);
                        try
                        {
                            await Task.Delay(delay, stoppingToken);
                        }
                        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                        {
                            throw;
                        }
                    }

                    await channel.BasicNackAsync(
                        args.DeliveryTag,
                        multiple: false,
                        requeue: true,
                        cancellationToken: stoppingToken);
                    return;

                case InboxClaimResult.Claimed:
                    break;

                default:
                    throw new InvalidOperationException($"Unexpected inbox claim result '{claimAttempt.Result}'.");
            }

            var claimToken = claimAttempt.Token
                ?? throw new InvalidOperationException(
                    $"Inbox claim for {message.EventId} returned Claimed without a fencing token.");

            try
            {
                await dispatcher.DispatchAsync(message, stoppingToken);
                await inbox.MarkProcessedAsync(claimToken, stoppingToken);
                await channel.BasicAckAsync(args.DeliveryTag, multiple: false, stoppingToken);
            }
            catch (Exception)
            {
                await inbox.ReleaseAsync(claimToken, clearClaimImmediately: true, stoppingToken);
                throw;
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed processing service event delivery {DeliveryTag} (EventId={EventId}).",
                args.DeliveryTag,
                message?.EventId);

            try
            {
                await HandleFailureAsync(channel, args, exception, CancellationToken.None);
            }
            catch (Exception failureException)
            {
                _logger.LogError(
                    failureException,
                    "Failed to apply retry/poison policy for delivery {DeliveryTag}.",
                    args.DeliveryTag);
            }
        }
    }

    private async Task HandleFailureAsync(
        IChannel channel,
        BasicDeliverEventArgs args,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var snapshot = _options.CurrentValue;
        var attempt = ReadDeliveryAttempt(args.BasicProperties?.Headers);

        if (snapshot.MaxDeliveryAttempts > 0 && attempt < snapshot.MaxDeliveryAttempts)
        {
            // Confirm the republish before ACK so a failed publish cannot drop the original delivery.
            // A crash between confirm and ACK can still duplicate (at-least-once; inbox required).
            try
            {
                await RepublishWithIncrementedAttemptAsync(channel, args, attempt + 1, snapshot, cancellationToken);
                await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken);
            }
            catch
            {
                await channel.BasicNackAsync(
                    args.DeliveryTag,
                    multiple: false,
                    requeue: true,
                    cancellationToken: cancellationToken);
                throw;
            }

            _logger.LogWarning(
                exception,
                "Requeued service event for retry {Attempt}/{MaxAttempts} (delivery {DeliveryTag}).",
                attempt + 1,
                snapshot.MaxDeliveryAttempts,
                args.DeliveryTag);
            return;
        }

        if (snapshot.MaxDeliveryAttempts > 0)
        {
            if (!HasDeadLetterConfigured(snapshot))
            {
                throw new InvalidOperationException(
                    "MaxDeliveryAttempts is greater than zero but no dead-letter exchange is configured. " +
                    "Set EnableDeadLetterTopology = true or DeadLetterExchangeName, or set MaxDeliveryAttempts = 0.");
            }

            try
            {
                await PublishToDeadLetterAsync(channel, args, attempt, exception, snapshot, cancellationToken);
                await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken);
            }
            catch
            {
                await channel.BasicNackAsync(
                    args.DeliveryTag,
                    multiple: false,
                    requeue: true,
                    cancellationToken: cancellationToken);
                throw;
            }

            _logger.LogError(
                exception,
                "Poisoned service event after {Attempts} attempts (delivery {DeliveryTag}); published to dead-letter.",
                attempt,
                args.DeliveryTag);
            return;
        }

        var requeue = snapshot.RequeueOnFailure;
        await channel.BasicNackAsync(
            args.DeliveryTag,
            multiple: false,
            requeue: requeue,
            cancellationToken: cancellationToken);
    }

    private static bool HasDeadLetterConfigured(RabbitMqServiceEventConsumerOptions snapshot)
        => snapshot.EnableDeadLetterTopology
           || !string.IsNullOrWhiteSpace(snapshot.DeadLetterExchangeName);

    private static async Task RepublishWithIncrementedAttemptAsync(
        IChannel channel,
        BasicDeliverEventArgs args,
        int nextAttempt,
        RabbitMqServiceEventConsumerOptions snapshot,
        CancellationToken cancellationToken)
    {
        var properties = CloneProperties(args.BasicProperties);
        properties.Headers ??= new Dictionary<string, object?>();
        properties.Headers[ServiceEventMessageHeaders.DeliveryAttempt] = nextAttempt;

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: snapshot.QueueName,
            mandatory: true,
            basicProperties: properties,
            body: args.Body,
            cancellationToken: cancellationToken);
    }

    private static async Task PublishToDeadLetterAsync(
        IChannel channel,
        BasicDeliverEventArgs args,
        int attempt,
        Exception exception,
        RabbitMqServiceEventConsumerOptions snapshot,
        CancellationToken cancellationToken)
    {
        var dlx = snapshot.ResolveDeadLetterExchangeName();
        var properties = CloneProperties(args.BasicProperties);
        properties.Headers ??= new Dictionary<string, object?>();
        properties.Headers[ServiceEventMessageHeaders.DeliveryAttempt] = attempt;
        properties.Headers[ServiceEventMessageHeaders.Poisoned] = true;
        properties.Headers["raccoonland-poison-reason"] = exception.Message;

        await channel.BasicPublishAsync(
            exchange: dlx,
            routingKey: snapshot.DeadLetterRoutingKey,
            mandatory: true,
            basicProperties: properties,
            body: args.Body,
            cancellationToken: cancellationToken);
    }

    private static BasicProperties CloneProperties(IReadOnlyBasicProperties? source)
    {
        var headers = source?.Headers is null
            ? new Dictionary<string, object?>()
            : new Dictionary<string, object?>(source.Headers);

        return new BasicProperties
        {
            MessageId = source?.MessageId,
            ContentType = source?.ContentType,
            ContentEncoding = source?.ContentEncoding,
            Type = source?.Type,
            Timestamp = source?.Timestamp ?? default,
            DeliveryMode = source?.DeliveryMode ?? DeliveryModes.Persistent,
            CorrelationId = source?.CorrelationId,
            Headers = headers,
        };
    }

    private static int ReadDeliveryAttempt(IDictionary<string, object?>? headers)
    {
        if (headers is null
            || !headers.TryGetValue(ServiceEventMessageHeaders.DeliveryAttempt, out var value)
            || value is null)
        {
            return 1;
        }

        return value switch
        {
            int i => i,
            long l => checked((int)l),
            byte b => b,
            byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), out var parsed) => parsed,
            string s when int.TryParse(s, out var parsed) => parsed,
            _ => int.TryParse(value.ToString(), out var parsed) ? parsed : 1,
        };
    }

    private static void ValidateRuntimeOptions(RabbitMqServiceEventConsumerOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.QueueName))
        {
            throw new InvalidOperationException("RabbitMqServiceEventConsumerOptions.QueueName is required.");
        }

        if (options.BindingKeys is null || options.BindingKeys.Length == 0)
        {
            throw new InvalidOperationException("RabbitMqServiceEventConsumerOptions.BindingKeys must contain at least one pattern.");
        }

        if (options.InboxClaimLease < TimeSpan.FromSeconds(1))
        {
            throw new InvalidOperationException("RabbitMqServiceEventConsumerOptions.InboxClaimLease must be at least one second.");
        }

        if (options.ClaimHeldByOtherRequeueDelay < TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                "RabbitMqServiceEventConsumerOptions.ClaimHeldByOtherRequeueDelay must be >= 0.");
        }

        if (options.MaxDeliveryAttempts < 0)
        {
            throw new InvalidOperationException("RabbitMqServiceEventConsumerOptions.MaxDeliveryAttempts must be >= 0.");
        }

        if (options.MaxDeliveryAttempts > 0
            && !options.EnableDeadLetterTopology
            && string.IsNullOrWhiteSpace(options.DeadLetterExchangeName))
        {
            throw new InvalidOperationException(
                "When MaxDeliveryAttempts > 0, EnableDeadLetterTopology must be true or DeadLetterExchangeName must be set.");
        }
    }

    private static ConnectionFactory CreateConnectionFactory(RabbitMqServiceEventConsumerOptions snapshot)
    {
        var factory = new ConnectionFactory
        {
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,
            ClientProvidedName = snapshot.ClientProvidedName,
        };

        if (!string.IsNullOrWhiteSpace(snapshot.Uri))
        {
            factory.Uri = new Uri(snapshot.Uri);
            return factory;
        }

        factory.HostName = snapshot.HostName;
        factory.Port = snapshot.Port;
        factory.UserName = snapshot.UserName;
        factory.Password = snapshot.Password;
        factory.VirtualHost = snapshot.VirtualHost;
        return factory;
    }
}
