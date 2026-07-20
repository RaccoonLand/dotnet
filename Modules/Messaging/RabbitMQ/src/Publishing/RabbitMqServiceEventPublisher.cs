using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RaccoonLand.Modules.Messaging.Abstractions;

namespace RaccoonLand.Modules.Messaging.RabbitMQ;

/// <summary>
/// Publishes claimed service-event outbox rows to a RabbitMQ exchange. The JSON payload is the same
/// body stored in the outbox; routing uses <see cref="OutboxEventRecord.EventType"/> by default.
/// Uses publisher confirms and <c>mandatory: true</c> so unroutable messages fail the publish
/// (outbox relay will not mark the row processed).
/// </summary>
public sealed class RabbitMqServiceEventPublisher : IServiceEventPublisher, IAsyncDisposable
{
    private readonly IOptionsMonitor<RabbitMqServiceEventOptions> _options;
    private readonly ILogger<RabbitMqServiceEventPublisher> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private IConnection? _connection;
    private IChannel? _channel;
    private bool _exchangeDeclared;
    private bool _disposed;

    public RabbitMqServiceEventPublisher(
        IOptionsMonitor<RabbitMqServiceEventOptions> options,
        ILogger<RabbitMqServiceEventPublisher> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task PublishAsync(OutboxEventRecord outboxEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(outboxEvent);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!string.Equals(outboxEvent.Category, OutboxEventCategory.Service, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"RabbitMqServiceEventPublisher only publishes Category '{OutboxEventCategory.Service}' " +
                $"(received '{outboxEvent.Category}' for EventId={outboxEvent.EventId}).");
        }

        var snapshot = _options.CurrentValue;
        var routingKey = BuildRoutingKey(snapshot.RoutingKeyFormat, outboxEvent);
        var body = Encoding.UTF8.GetBytes(outboxEvent.Payload);
        var properties = BuildProperties(snapshot, outboxEvent);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var channel = await EnsureChannelAsync(snapshot, cancellationToken);

            // mandatory + publisher confirmation tracking: await throws when the broker nacks
            // or returns the message (no queue bound for the routing key).
            await channel.BasicPublishAsync(
                exchange: snapshot.ExchangeName,
                routingKey: routingKey,
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            _logger.LogDebug(
                "Published service event {EventType} ({EventId}) to exchange {Exchange} with routing key {RoutingKey}.",
                outboxEvent.EventType,
                outboxEvent.EventId,
                snapshot.ExchangeName,
                routingKey);
        }
        catch
        {
            await ResetChannelAsync();
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<IChannel> EnsureChannelAsync(
        RabbitMqServiceEventOptions snapshot,
        CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true } && _connection is { IsOpen: true })
        {
            if (snapshot.DeclareExchange && !_exchangeDeclared)
            {
                await DeclareExchangeAsync(_channel, snapshot, cancellationToken);
            }

            return _channel;
        }

        await ResetChannelAsync();

        var factory = CreateConnectionFactory(snapshot);
        _connection = await factory.CreateConnectionAsync(cancellationToken);

        var channelOptions = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);
        _channel = await _connection.CreateChannelAsync(channelOptions, cancellationToken);

        if (snapshot.DeclareExchange)
        {
            await DeclareExchangeAsync(_channel, snapshot, cancellationToken);
        }

        return _channel;
    }

    private static ConnectionFactory CreateConnectionFactory(RabbitMqServiceEventOptions snapshot)
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

    private async Task DeclareExchangeAsync(
        IChannel channel,
        RabbitMqServiceEventOptions snapshot,
        CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            exchange: snapshot.ExchangeName,
            type: snapshot.ExchangeType,
            durable: snapshot.DurableExchange,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        _exchangeDeclared = true;
    }

    private static BasicProperties BuildProperties(
        RabbitMqServiceEventOptions snapshot,
        OutboxEventRecord outboxEvent)
    {
        var headers = new Dictionary<string, object?>
        {
            [ServiceEventMessageHeaders.EventId] = outboxEvent.EventId.ToString(),
            [ServiceEventMessageHeaders.EventType] = outboxEvent.EventType,
            [ServiceEventMessageHeaders.Category] = outboxEvent.Category,
            [ServiceEventMessageHeaders.AggregateType] = outboxEvent.AggregateType,
            [ServiceEventMessageHeaders.AggregateBusinessKey] = outboxEvent.AggregateBusinessKey.ToString(),
            [ServiceEventMessageHeaders.OccurredOnUtc] = outboxEvent.OccurredOnUtc.ToString("O"),
        };

        if (!string.IsNullOrWhiteSpace(outboxEvent.CreatedBy))
        {
            headers[ServiceEventMessageHeaders.CreatedBy] = outboxEvent.CreatedBy;
        }

        var eventVersion = TryReadEventVersion(outboxEvent.Payload);
        if (eventVersion is not null)
        {
            headers[ServiceEventMessageHeaders.EventVersion] = eventVersion.Value;
        }

        return new BasicProperties
        {
            MessageId = outboxEvent.EventId.ToString(),
            ContentType = "application/json",
            ContentEncoding = "utf-8",
            Type = outboxEvent.EventType,
            Timestamp = new AmqpTimestamp(outboxEvent.OccurredOnUtc.ToUnixTimeSeconds()),
            DeliveryMode = snapshot.PersistentMessages ? DeliveryModes.Persistent : DeliveryModes.Transient,
            Headers = headers,
        };
    }

    private static int? TryReadEventVersion(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            if (document.RootElement.TryGetProperty("eventVersion", out var camel)
                && camel.TryGetInt32(out var camelValue))
            {
                return camelValue;
            }

            if (document.RootElement.TryGetProperty("EventVersion", out var pascal)
                && pascal.TryGetInt32(out var pascalValue))
            {
                return pascalValue;
            }
        }
        catch (JsonException)
        {
            // Payload remains the body; version header is optional metadata.
        }

        return null;
    }

    private static string BuildRoutingKey(string format, OutboxEventRecord outboxEvent)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return outboxEvent.EventType;
        }

        return format
            .Replace("{EventType}", outboxEvent.EventType, StringComparison.Ordinal)
            .Replace("{Category}", outboxEvent.Category, StringComparison.Ordinal)
            .Replace("{AggregateType}", outboxEvent.AggregateType, StringComparison.Ordinal);
    }

    private async Task ResetChannelAsync()
    {
        _exchangeDeclared = false;

        if (_channel is not null)
        {
            try
            {
                await _channel.DisposeAsync();
            }
            catch (Exception exception)
            {
                _logger.LogDebug(exception, "Error disposing RabbitMQ channel during reset.");
            }

            _channel = null;
        }

        if (_connection is not null)
        {
            try
            {
                await _connection.DisposeAsync();
            }
            catch (Exception exception)
            {
                _logger.LogDebug(exception, "Error disposing RabbitMQ connection during reset.");
            }

            _connection = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await _gate.WaitAsync();
        try
        {
            await ResetChannelAsync();
        }
        finally
        {
            _gate.Release();
            _gate.Dispose();
        }
    }
}
