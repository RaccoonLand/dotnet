using RaccoonLand.Modules.Persistence.Outbox.Abstraction;

namespace RaccoonLand.Modules.Persistence.SqlServer.Commands.Outbox;

/// <summary>Maps registered outbox channel markers to their SQL Server storage options.</summary>
public interface IOutboxChannelRegistry
{
    /// <summary>Returns the options for <typeparamref name="TOutbox"/>, or null when not registered.</summary>
    OutboxChannelOptions? Get<TOutbox>() where TOutbox : IOutbox;

    /// <summary>Returns the options for the given channel marker type, or null when not registered.</summary>
    OutboxChannelOptions? Get(Type channelType);

    /// <summary>All registered channel markers.</summary>
    IReadOnlyCollection<Type> Channels { get; }
}
