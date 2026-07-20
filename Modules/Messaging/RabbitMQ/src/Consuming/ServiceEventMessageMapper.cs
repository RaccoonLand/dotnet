using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RaccoonLand.Modules.Messaging.Abstractions;

namespace RaccoonLand.Modules.Messaging.RabbitMQ;

/// <summary>
/// Maps an AMQP delivery to <see cref="ServiceEventMessage"/> using publisher header conventions.
/// </summary>
public static class ServiceEventMessageMapper
{
    public static ServiceEventMessage FromDelivery(BasicDeliverEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var body = Encoding.UTF8.GetString(args.Body.ToArray());
        var props = args.BasicProperties;
        var headers = props?.Headers;

        var eventId = ReadGuidHeader(headers, ServiceEventMessageHeaders.EventId)
            ?? (Guid.TryParse(props?.MessageId, out var fromMessageId) ? fromMessageId : (Guid?)null)
            ?? TryReadGuidFromPayload(body, "eventId", "EventId")
            ?? throw new InvalidOperationException(
                "Service event delivery is missing EventId (header, message-id, or payload).");

        var eventType = ReadStringHeader(headers, ServiceEventMessageHeaders.EventType)
            ?? props?.Type
            ?? TryReadStringFromPayload(body, "eventType", "EventType")
            ?? throw new InvalidOperationException(
                $"Service event {eventId} is missing EventType (header, type property, or payload).");

        var eventVersion = ReadIntHeader(headers, ServiceEventMessageHeaders.EventVersion)
            ?? TryReadIntFromPayload(body, "eventVersion", "EventVersion");

        Guid? aggregateKey = ReadGuidHeader(headers, ServiceEventMessageHeaders.AggregateBusinessKey);
        DateTimeOffset? occurredOn = ReadDateTimeOffsetHeader(headers, ServiceEventMessageHeaders.OccurredOnUtc);

        return new ServiceEventMessage
        {
            EventId = eventId,
            EventType = eventType,
            EventVersion = eventVersion,
            Payload = body,
            AggregateType = ReadStringHeader(headers, ServiceEventMessageHeaders.AggregateType),
            AggregateBusinessKey = aggregateKey,
            OccurredOnUtc = occurredOn,
            CreatedBy = ReadStringHeader(headers, ServiceEventMessageHeaders.CreatedBy),
            RoutingKey = args.RoutingKey,
        };
    }

    private static string? ReadStringHeader(IDictionary<string, object?>? headers, string key)
    {
        if (headers is null || !headers.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            string s => s,
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            ReadOnlyMemory<byte> memory => Encoding.UTF8.GetString(memory.Span),
            _ => value.ToString(),
        };
    }

    private static Guid? ReadGuidHeader(IDictionary<string, object?>? headers, string key)
    {
        var text = ReadStringHeader(headers, key);
        return Guid.TryParse(text, out var guid) ? guid : null;
    }

    private static int? ReadIntHeader(IDictionary<string, object?>? headers, string key)
    {
        if (headers is null || !headers.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            int i => i,
            long l => checked((int)l),
            byte b => b,
            byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), out var parsed) => parsed,
            string s when int.TryParse(s, out var parsed) => parsed,
            _ => int.TryParse(value.ToString(), out var parsed) ? parsed : null,
        };
    }

    private static DateTimeOffset? ReadDateTimeOffsetHeader(IDictionary<string, object?>? headers, string key)
    {
        var text = ReadStringHeader(headers, key);
        return DateTimeOffset.TryParse(text, out var value) ? value : null;
    }

    private static Guid? TryReadGuidFromPayload(string payload, params string[] names)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            foreach (var name in names)
            {
                if (document.RootElement.TryGetProperty(name, out var property)
                    && property.ValueKind == JsonValueKind.String
                    && Guid.TryParse(property.GetString(), out var guid))
                {
                    return guid;
                }
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }

    private static string? TryReadStringFromPayload(string payload, params string[] names)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            foreach (var name in names)
            {
                if (document.RootElement.TryGetProperty(name, out var property)
                    && property.ValueKind == JsonValueKind.String)
                {
                    return property.GetString();
                }
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }

    private static int? TryReadIntFromPayload(string payload, params string[] names)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            foreach (var name in names)
            {
                if (document.RootElement.TryGetProperty(name, out var property)
                    && property.TryGetInt32(out var value))
                {
                    return value;
                }
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }
}
