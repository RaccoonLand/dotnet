using System.Collections.Concurrent;
using RaccoonLand.Modules.Persistence.Outbox.Abstraction;

namespace RaccoonLand.Modules.Persistence.SqlServer.Commands.Outbox;

/// <summary>Singleton registry of outbox channels registered at startup.</summary>
public sealed class OutboxChannelRegistry : IOutboxChannelRegistry
{
    private readonly ConcurrentDictionary<Type, OutboxChannelOptions> _channels = new();

    public IReadOnlyCollection<Type> Channels => _channels.Keys.ToArray();

    public void Register<TOutbox>(OutboxChannelOptions options) where TOutbox : IOutbox
        => Register(typeof(TOutbox), options);

    public void Register(Type channelType, OutboxChannelOptions options)
    {
        ArgumentNullException.ThrowIfNull(channelType);
        ArgumentNullException.ThrowIfNull(options);

        if (!typeof(IOutbox).IsAssignableFrom(channelType))
        {
            throw new ArgumentException(
                $"Type '{channelType.FullName}' must implement {nameof(IOutbox)}.",
                nameof(channelType));
        }

        if (string.IsNullOrWhiteSpace(options.Table))
        {
            throw new ArgumentException("Outbox table name is required.", nameof(options));
        }

        _channels[channelType] = options;
    }

    public OutboxChannelOptions? Get<TOutbox>() where TOutbox : IOutbox
        => Get(typeof(TOutbox));

    public OutboxChannelOptions? Get(Type channelType)
        => _channels.TryGetValue(channelType, out var options) ? options : null;
}
