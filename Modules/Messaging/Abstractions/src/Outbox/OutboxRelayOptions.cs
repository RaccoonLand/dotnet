namespace RaccoonLand.Modules.Messaging.Abstractions;

/// <summary>
/// Options for the outbox relay worker that polls unpublished events and dispatches or publishes them.
/// </summary>
public sealed class OutboxRelayOptions
{
    /// <summary>Default root configuration section name (<c>OutboxRelay</c>).</summary>
    public const string SectionName = "OutboxRelay";

    /// <summary>Maximum rows claimed per poll.</summary>
    public int BatchSize { get; set; } = 20;

    /// <summary>Delay between polls when the previous batch was empty or after a full cycle.</summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Exclusive claim duration passed to <see cref="IOutboxEventStore.ClaimPendingAsync"/>.
    /// After expiry another relay instance may reclaim unfinished rows (at-least-once).
    /// </summary>
    public TimeSpan ClaimLease { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// When true, rows with <see cref="OutboxEventCategory.Domain"/> are dispatched via
    /// <see cref="IDomainEventDispatcher"/> inside this service (asynchronous to the originating request).
    /// </summary>
    public bool ProcessDomainEvents { get; set; } = true;

    /// <summary>
    /// When true, rows with <see cref="OutboxEventCategory.Service"/> are published via
    /// <see cref="IServiceEventPublisher"/>. Requires a registered publisher (for example the
    /// RabbitMQ adapter). Defaults to false so Service events stay pending until a broker is configured.
    /// </summary>
    public bool ProcessServiceEvents { get; set; } = false;
}
