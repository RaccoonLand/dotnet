using System.Text.Json;
using RaccoonLand.Core.ExecutionContext.Abstractions;
using RaccoonLand.Modules.Persistence.Outbox.Abstraction;

namespace RaccoonLand.Modules.Persistence.SqlServer.Commands.Outbox;

/// <summary>A batch of pending messages targeting one outbox channel.</summary>
internal sealed record OutboxChannelBatch(Type ChannelType, IReadOnlyList<OutboxPendingMessage> Messages);

/// <summary>Request-scoped buffer drained by <see cref="Interceptors.OutboxWriterSaveChangesInterceptor"/>.</summary>
public sealed class OutboxWriter(
    IOutboxChannelRegistry registry,
    ICurrentExecutionContext? executionContext = null) : IOutboxWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IOutboxChannelRegistry _registry = registry;
    private readonly ICurrentExecutionContext _executionContext =
        executionContext ?? NullCurrentExecutionContext.Instance;
    private readonly List<(Type ChannelType, OutboxPendingMessage Message)> _pending = [];

    public void Enqueue<TOutbox>(object payload, OutboxEnqueueOptions? options = null)
        where TOutbox : IOutbox
    {
        ArgumentNullException.ThrowIfNull(payload);

        if (_registry.Get<TOutbox>() is null)
        {
            throw new InvalidOperationException(
                $"Outbox channel '{typeof(TOutbox).FullName}' is not registered. " +
                $"Call AddRaccoonLandOutbox<{typeof(TOutbox).Name}>() during startup.");
        }

        var payloadType = payload.GetType();
        var message = new OutboxPendingMessage
        {
            Id = Guid.CreateVersion7(),
            EventType = string.IsNullOrWhiteSpace(options?.EventType) ? payloadType.Name : options.EventType,
            AggregateType = options?.AggregateType,
            AggregateBusinessKey = options?.AggregateBusinessKey,
            Payload = JsonSerializer.Serialize(payload, payloadType, JsonOptions),
            CreatedBy = _executionContext.IsAvailable ? _executionContext.UserId : null,
            OccurredOnUtc = DateTimeOffset.UtcNow,
        };

        _pending.Add((typeof(TOutbox), message));
    }

    /// <summary>
    /// Returns the current buffer grouped by channel without clearing. Call
    /// <see cref="ClearPending"/> only after a successful flush so a failed write can retry.
    /// </summary>
    internal IReadOnlyList<OutboxChannelBatch> GetPendingBatches()
    {
        if (_pending.Count == 0)
        {
            return [];
        }

        return _pending
            .GroupBy(item => item.ChannelType)
            .Select(group => new OutboxChannelBatch(
                group.Key,
                group.Select(item => item.Message).ToArray()))
            .ToArray();
    }

    /// <summary>Clears the buffer after messages have been successfully written.</summary>
    internal void ClearPending() => _pending.Clear();
}
