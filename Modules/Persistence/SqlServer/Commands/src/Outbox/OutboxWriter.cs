using System.Text.Json;
using RaccoonLand.Core.ExecutionContext.Abstractions;
using RaccoonLand.Modules.Persistence.Outbox.Abstraction;

namespace RaccoonLand.Modules.Persistence.SqlServer.Commands.Outbox;

/// <summary>A batch of pending messages targeting one outbox channel.</summary>
internal sealed record OutboxChannelBatch(Type ChannelType, IReadOnlyList<OutboxPendingMessage> Messages);

/// <summary>
/// Request-scoped buffer flushed by <see cref="Interceptors.OutboxWriterSaveChangesInterceptor"/>.
/// Messages move to an awaiting-commit set when written, and are dropped only after the ambient transaction
/// commits (or immediately when writing with no transaction).
/// </summary>
public sealed class OutboxWriter(
    IOutboxChannelRegistry registry,
    ICurrentExecutionContext? executionContext = null) : IOutboxWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IOutboxChannelRegistry _registry = registry;
    private readonly ICurrentExecutionContext _executionContext =
        executionContext ?? NullCurrentExecutionContext.Instance;
    private readonly List<(Type ChannelType, OutboxPendingMessage Message)> _pending = [];
    private readonly List<(Type ChannelType, OutboxPendingMessage Message)> _awaitingCommit = [];

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

    /// <summary>Returns not-yet-flushed messages grouped by channel.</summary>
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

    /// <summary>
    /// Moves the given message ids from the pending buffer to the awaiting-commit set so a later
    /// <c>SaveChanges</c> in the same transaction does not insert them again.
    /// </summary>
    internal void MarkFlushed(IReadOnlyCollection<Guid> messageIds)
    {
        if (messageIds.Count == 0)
        {
            return;
        }

        var idSet = messageIds as HashSet<Guid> ?? messageIds.ToHashSet();
        for (var index = _pending.Count - 1; index >= 0; index--)
        {
            var item = _pending[index];
            if (!idSet.Contains(item.Message.Id))
            {
                continue;
            }

            _awaitingCommit.Add(item);
            _pending.RemoveAt(index);
        }
    }

    /// <summary>Drops messages that were flushed in the committed transaction.</summary>
    internal void ClearFlushedOnCommit() => _awaitingCommit.Clear();

    /// <summary>After rollback, restores flushed messages so a retry can insert them again.</summary>
    internal void RestoreFlushedOnRollback()
    {
        if (_awaitingCommit.Count == 0)
        {
            return;
        }

        _pending.AddRange(_awaitingCommit);
        _awaitingCommit.Clear();
    }

    /// <summary>Clears all buffers (used when writing outside an ambient transaction).</summary>
    internal void ClearPending()
    {
        _pending.Clear();
        _awaitingCommit.Clear();
    }
}
