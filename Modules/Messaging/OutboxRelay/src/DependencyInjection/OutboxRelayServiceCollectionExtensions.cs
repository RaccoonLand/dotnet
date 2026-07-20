using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RaccoonLand.Core.Domain.Events;
using RaccoonLand.Modules.Messaging.Abstractions;

namespace RaccoonLand.Modules.Messaging.OutboxRelay;

/// <summary>
/// DI registration for the domain-event dispatcher and outbox relay hosted service.
/// </summary>
public static class OutboxRelayServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IDomainEventDispatcher"/>, options, and <see cref="OutboxRelayBackgroundService"/>.
    /// Does not register <see cref="IOutboxEventStore"/> — call the SQL Server (or other) store extension separately.
    /// </summary>
    public static IServiceCollection AddRaccoonLandOutboxRelay(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = OutboxRelayOptions.SectionName,
        Action<OutboxRelayOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        var builder = services.AddOptions<OutboxRelayOptions>()
            .Bind(configuration.GetSection(sectionName));

        if (configure is not null)
        {
            builder.Configure(configure);
        }

        ValidateRelayOptions(builder);
        AddRelayCore(services);
        return services;
    }

    /// <summary>
    /// Registers the relay with code-only options.
    /// </summary>
    public static IServiceCollection AddRaccoonLandOutboxRelay(
        this IServiceCollection services,
        Action<OutboxRelayOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var builder = services.AddOptions<OutboxRelayOptions>();
        if (configure is not null)
        {
            builder.Configure(configure);
        }

        ValidateRelayOptions(builder);
        AddRelayCore(services);
        return services;
    }

    /// <summary>
    /// Registers <typeparamref name="THandler"/> for <typeparamref name="TEvent"/>. The stable
    /// <c>EventType</c> is resolved from an uninitialized <typeparamref name="TEvent"/> instance
    /// (the override must return a constant contract string).
    /// </summary>
    public static IServiceCollection AddRaccoonLandDomainEventHandler<TEvent, THandler>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TEvent : DomainEvent
        where THandler : class, IDomainEventHandler<TEvent>
    {
        ArgumentNullException.ThrowIfNull(services);

        var eventType = ResolveEventType<TEvent>();
        services.AddSingleton(new DomainEventHandlerRegistration
        {
            EventType = eventType,
            EventClrType = typeof(TEvent),
            HandlerServiceType = typeof(IDomainEventHandler<TEvent>),
        });

        services.Add(new ServiceDescriptor(typeof(IDomainEventHandler<TEvent>), typeof(THandler), lifetime));
        services.Add(new ServiceDescriptor(typeof(THandler), typeof(THandler), lifetime));

        return services;
    }

    private static void AddRelayCore(IServiceCollection services)
    {
        services.TryAddSingleton<DomainEventHandlerRegistry>(sp =>
        {
            var registry = new DomainEventHandlerRegistry();
            foreach (var registration in sp.GetServices<DomainEventHandlerRegistration>())
            {
                registry.Add(registration);
            }

            return registry;
        });

        services.TryAddSingleton<IDomainEventDispatcher, DomainEventDispatcher>();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IHostedService, OutboxRelayBackgroundService>());
    }

    private static void ValidateRelayOptions(OptionsBuilder<OutboxRelayOptions> builder)
    {
        builder.Validate(
                options => options.BatchSize > 0
                    && options.PollInterval >= TimeSpan.Zero
                    && options.ClaimLease >= TimeSpan.FromSeconds(1),
                "OutboxRelay BatchSize must be > 0, PollInterval must be >= 0, and ClaimLease must be >= 00:00:01.")
            .ValidateOnStart();
    }

    private static string ResolveEventType<TEvent>()
        where TEvent : DomainEvent
    {
        var probe = (TEvent)RuntimeHelpers.GetUninitializedObject(typeof(TEvent));
        var eventType = probe.EventType;
        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new InvalidOperationException(
                $"Domain event type {typeof(TEvent).FullName} returned an empty EventType.");
        }

        return eventType;
    }
}
