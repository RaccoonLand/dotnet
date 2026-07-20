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
        builder.Validate(static options =>
            {
                if (string.IsNullOrWhiteSpace(options.ExchangeName)
                    || string.IsNullOrWhiteSpace(options.ExchangeType)
                    || string.IsNullOrWhiteSpace(options.RoutingKeyFormat))
                {
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(options.Uri))
                {
                    return Uri.TryCreate(options.Uri, UriKind.Absolute, out var uri)
                        && (uri.Scheme == "amqp" || uri.Scheme == "amqps");
                }

                return !string.IsNullOrWhiteSpace(options.HostName)
                    && options.Port > 0
                    && !string.IsNullOrWhiteSpace(options.UserName)
                    && !string.IsNullOrWhiteSpace(options.VirtualHost);
            },
            "RabbitMqServiceEvents requires ExchangeName/ExchangeType/RoutingKeyFormat, and either a valid amqp(s) Uri or HostName/Port/UserName/VirtualHost.")
            .ValidateOnStart();
    }

    private static void ValidateConsumerOptions(OptionsBuilder<RabbitMqServiceEventConsumerOptions> builder)
    {
        builder.Validate(static options =>
            {
                if (string.IsNullOrWhiteSpace(options.ExchangeName)
                    || string.IsNullOrWhiteSpace(options.ExchangeType)
                    || string.IsNullOrWhiteSpace(options.QueueName)
                    || options.BindingKeys is null
                    || options.BindingKeys.Length == 0
                    || options.PrefetchCount == 0
                    || options.InboxClaimLease < TimeSpan.FromSeconds(1)
                    || options.ClaimHeldByOtherRequeueDelay < TimeSpan.Zero
                    || options.MaxDeliveryAttempts < 0)
                {
                    return false;
                }

                if (options.MaxDeliveryAttempts > 0
                    && !options.EnableDeadLetterTopology
                    && string.IsNullOrWhiteSpace(options.DeadLetterExchangeName))
                {
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(options.Uri))
                {
                    return Uri.TryCreate(options.Uri, UriKind.Absolute, out var uri)
                        && (uri.Scheme == "amqp" || uri.Scheme == "amqps");
                }

                return !string.IsNullOrWhiteSpace(options.HostName)
                    && options.Port > 0
                    && !string.IsNullOrWhiteSpace(options.UserName)
                    && !string.IsNullOrWhiteSpace(options.VirtualHost);
            },
            "RabbitMqServiceEventConsumer requires QueueName, BindingKeys, PrefetchCount > 0, InboxClaimLease >= 00:00:01, " +
            "ClaimHeldByOtherRequeueDelay >= 0, and when MaxDeliveryAttempts > 0 a dead-letter exchange " +
            "(EnableDeadLetterTopology or DeadLetterExchangeName), plus exchange settings and Uri or host credentials.")
            .ValidateOnStart();
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
