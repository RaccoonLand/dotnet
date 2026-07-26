using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RaccoonLand.Core.Domain.Events;
using RaccoonLand.Modules.Messaging.Abstractions;

namespace RaccoonLand.Modules.Messaging.RabbitMQ;

/// <summary>
/// DI registration for RabbitMQ service-event publish and consume adapters.
/// </summary>
public static class RabbitMqServiceEventServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="RabbitMqServiceEventPublisher"/> as <see cref="IServiceEventPublisher"/>
    /// and binds <see cref="RabbitMqServiceEventOptions"/> from configuration.
    /// Enable <c>OutboxRelay:ProcessServiceEvents</c> so the relay calls the publisher.
    /// </summary>
    public static IServiceCollection AddRaccoonLandRabbitMqServiceEvents(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = RabbitMqServiceEventOptions.SectionName,
        Action<RabbitMqServiceEventOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        var builder = services.AddOptions<RabbitMqServiceEventOptions>()
            .Bind(configuration.GetSection(sectionName));

        if (configure is not null)
        {
            builder.Configure(configure);
        }

        ValidatePublisherOptions(builder);
        RegisterPublisher(services);
        return services;
    }

    /// <summary>
    /// Registers the RabbitMQ publisher with code-only options.
    /// </summary>
    public static IServiceCollection AddRaccoonLandRabbitMqServiceEvents(
        this IServiceCollection services,
        Action<RabbitMqServiceEventOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = services.AddOptions<RabbitMqServiceEventOptions>()
            .Configure(configure);

        ValidatePublisherOptions(builder);
        RegisterPublisher(services);
        return services;
    }

    /// <summary>
    /// Registers the RabbitMQ service-event consumer hosted service, dispatcher, and consumer options.
    /// Requires <see cref="IInboxStore"/> (for example <c>AddRaccoonLandInboxStore</c>).
    /// </summary>
    public static IServiceCollection AddRaccoonLandRabbitMqServiceEventConsumer(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = RabbitMqServiceEventConsumerOptions.SectionName,
        Action<RabbitMqServiceEventConsumerOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        var builder = services.AddOptions<RabbitMqServiceEventConsumerOptions>()
            .Bind(configuration.GetSection(sectionName));

        if (configure is not null)
        {
            builder.Configure(configure);
        }

        ValidateConsumerOptions(builder);
        RegisterConsumerCore(services);
        return services;
    }

    /// <summary>
    /// Registers the RabbitMQ service-event consumer with code-only options.
    /// </summary>
    public static IServiceCollection AddRaccoonLandRabbitMqServiceEventConsumer(
        this IServiceCollection services,
        Action<RabbitMqServiceEventConsumerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = services.AddOptions<RabbitMqServiceEventConsumerOptions>()
            .Configure(configure);

        ValidateConsumerOptions(builder);
        RegisterConsumerCore(services);
        return services;
    }

    /// <summary>
    /// Registers <typeparamref name="THandler"/> for <typeparamref name="TEvent"/>. The stable
    /// <c>EventType</c> is resolved from an uninitialized <typeparamref name="TEvent"/> instance.
    /// </summary>
    public static IServiceCollection AddRaccoonLandServiceEventHandler<TEvent, THandler>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TEvent : ServiceEvent
        where THandler : class, IServiceEventHandler<TEvent>
    {
        ArgumentNullException.ThrowIfNull(services);

        var eventType = ResolveEventType<TEvent>();
        services.AddSingleton(new ServiceEventHandlerRegistration
        {
            EventType = eventType,
            EventClrType = typeof(TEvent),
            HandlerServiceType = typeof(IServiceEventHandler<TEvent>),
        });

        services.Add(new ServiceDescriptor(typeof(IServiceEventHandler<TEvent>), typeof(THandler), lifetime));
        services.Add(new ServiceDescriptor(typeof(THandler), typeof(THandler), lifetime));

        return services;
    }

    private static void RegisterPublisher(IServiceCollection services)
    {
        services.TryAddSingleton<RabbitMqServiceEventPublisher>();
        services.TryAddSingleton<IServiceEventPublisher>(sp => sp.GetRequiredService<RabbitMqServiceEventPublisher>());
    }

    private static void RegisterConsumerCore(IServiceCollection services)
    {
        services.TryAddSingleton<ServiceEventHandlerRegistry>(sp =>
        {
            var registry = new ServiceEventHandlerRegistry();
            foreach (var registration in sp.GetServices<ServiceEventHandlerRegistration>())
            {
                registry.Add(registration);
            }

            return registry;
        });

        services.TryAddSingleton<IServiceEventDispatcher, ServiceEventDispatcher>();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IHostedService, RabbitMqServiceEventConsumerBackgroundService>());
    }

    private static void ValidatePublisherOptions(OptionsBuilder<RabbitMqServiceEventOptions> builder)
    {
        const string section = RabbitMqServiceEventOptions.SectionName;
        builder
            .Validate(
                static o => !string.IsNullOrWhiteSpace(o.ExchangeName),
                $"{section}.{nameof(RabbitMqServiceEventOptions.ExchangeName)} is required.")
            .Validate(
                static o => !string.IsNullOrWhiteSpace(o.ExchangeType),
                $"{section}.{nameof(RabbitMqServiceEventOptions.ExchangeType)} is required.")
            .Validate(
                static o => !string.IsNullOrWhiteSpace(o.RoutingKeyFormat),
                $"{section}.{nameof(RabbitMqServiceEventOptions.RoutingKeyFormat)} is required.")
            .Validate(
                static o => IsValidAmqpConnection(o.Uri, o.HostName, o.Port, o.UserName, o.VirtualHost),
                $"{section} requires either a valid amqp(s) Uri or a full set of HostName/Port/UserName/VirtualHost.")
            .ValidateOnStart();
    }

    private static void ValidateConsumerOptions(OptionsBuilder<RabbitMqServiceEventConsumerOptions> builder)
    {
        const string section = RabbitMqServiceEventConsumerOptions.SectionName;
        builder
            .Validate(
                static o => !string.IsNullOrWhiteSpace(o.ExchangeName),
                $"{section}.{nameof(RabbitMqServiceEventConsumerOptions.ExchangeName)} is required.")
            .Validate(
                static o => !string.IsNullOrWhiteSpace(o.ExchangeType),
                $"{section}.{nameof(RabbitMqServiceEventConsumerOptions.ExchangeType)} is required.")
            .Validate(
                static o => !string.IsNullOrWhiteSpace(o.QueueName),
                $"{section}.{nameof(RabbitMqServiceEventConsumerOptions.QueueName)} is required.")
            .Validate(
                static o => o.BindingKeys is not null && o.BindingKeys.Length > 0,
                $"{section}.{nameof(RabbitMqServiceEventConsumerOptions.BindingKeys)} must contain at least one entry.")
            .Validate(
                static o => o.PrefetchCount > 0,
                $"{section}.{nameof(RabbitMqServiceEventConsumerOptions.PrefetchCount)} must be greater than zero.")
            .Validate(
                static o => o.InboxClaimLease >= TimeSpan.FromSeconds(1),
                $"{section}.{nameof(RabbitMqServiceEventConsumerOptions.InboxClaimLease)} must be greater than or equal to 00:00:01.")
            .Validate(
                static o => o.ClaimHeldByOtherRequeueDelay >= TimeSpan.Zero,
                $"{section}.{nameof(RabbitMqServiceEventConsumerOptions.ClaimHeldByOtherRequeueDelay)} must be greater than or equal to 00:00:00.")
            .Validate(
                static o => o.MaxDeliveryAttempts >= 0,
                $"{section}.{nameof(RabbitMqServiceEventConsumerOptions.MaxDeliveryAttempts)} must be greater than or equal to zero.")
            .Validate(
                static o => o.MaxDeliveryAttempts == 0
                    || o.EnableDeadLetterTopology
                    || !string.IsNullOrWhiteSpace(o.DeadLetterExchangeName),
                $"{section}: when {nameof(RabbitMqServiceEventConsumerOptions.MaxDeliveryAttempts)} > 0 a dead-letter exchange is required " +
                $"(set {nameof(RabbitMqServiceEventConsumerOptions.EnableDeadLetterTopology)}=true or provide {nameof(RabbitMqServiceEventConsumerOptions.DeadLetterExchangeName)}).")
            .Validate(
                static o => IsValidAmqpConnection(o.Uri, o.HostName, o.Port, o.UserName, o.VirtualHost),
                $"{section} requires either a valid amqp(s) Uri or a full set of HostName/Port/UserName/VirtualHost.")
            .ValidateOnStart();
    }

    private static bool IsValidAmqpConnection(string? uri, string hostName, int port, string userName, string virtualHost)
    {
        if (!string.IsNullOrWhiteSpace(uri))
        {
            return System.Uri.TryCreate(uri, UriKind.Absolute, out var parsed)
                && (parsed.Scheme == "amqp" || parsed.Scheme == "amqps");
        }

        return !string.IsNullOrWhiteSpace(hostName)
            && port > 0
            && !string.IsNullOrWhiteSpace(userName)
            && !string.IsNullOrWhiteSpace(virtualHost);
    }

    private static string ResolveEventType<TEvent>()
        where TEvent : ServiceEvent
    {
        var probe = (TEvent)RuntimeHelpers.GetUninitializedObject(typeof(TEvent));
        var eventType = probe.EventType;
        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new InvalidOperationException(
                $"Service event type {typeof(TEvent).FullName} returned an empty EventType.");
        }

        return eventType;
    }
}
