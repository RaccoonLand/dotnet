using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaccoonLand.Core.ExecutionContext.Abstractions;
using RaccoonLand.Modules.Persistence.Outbox.Abstraction;
using RaccoonLand.Modules.Persistence.SqlServer.Commands.Interceptors;
using RaccoonLand.Modules.Persistence.SqlServer.Commands.Outbox;

namespace RaccoonLand.Modules.Persistence.SqlServer.Commands;

/// <summary>Registration for the transactional outbox writer (request-scoped writer + SaveChanges interceptor).</summary>
public static class OutboxServiceCollectionExtensions
{
    /// <summary>
    /// Registers the outbox infrastructure: channel registry, request-scoped writer, and the SaveChanges
    /// interceptor. Call <see cref="AddRaccoonLandOutbox{TOutbox}"/> for each channel, then add the interceptor
    /// to the DbContext options via
    /// <see cref="CommandDbContextOptionsBuilderExtensions.AddRaccoonLandOutboxInterceptor"/>.
    /// </summary>
    public static IServiceCollection AddRaccoonLandOutbox(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<OutboxChannelRegistry>();
        EnsureRegistryFactory(services);

        // The writer stamps CreatedBy from ICurrentExecutionContext. When no implementation is registered,
        // fall back to NullCurrentExecutionContext (CreatedBy stays null), mirroring the audit interceptor.
        services.TryAddScoped<OutboxWriter>(sp => new OutboxWriter(
            sp.GetRequiredService<IOutboxChannelRegistry>(),
            sp.GetService<ICurrentExecutionContext>()));
        services.TryAddScoped<IOutboxWriter>(sp => sp.GetRequiredService<OutboxWriter>());
        services.TryAddScoped<OutboxWriterSaveChangesInterceptor>();

        return services;
    }

    /// <summary>
    /// Registers an outbox channel marker (an application-defined <see cref="IOutbox"/>) and its table
    /// location. The base infrastructure is wired up automatically.
    /// </summary>
    public static IServiceCollection AddRaccoonLandOutbox<TOutbox>(
        this IServiceCollection services,
        Action<OutboxChannelOptions> configure)
        where TOutbox : IOutbox
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddRaccoonLandOutbox();

        services.AddSingleton<Action<OutboxChannelRegistry>>(_ => registry =>
        {
            var options = new OutboxChannelOptions();
            configure(options);
            registry.Register<TOutbox>(options);
        });

        return services;
    }

    private static void EnsureRegistryFactory(IServiceCollection services)
    {
        if (services.Any(descriptor => descriptor.ServiceType == typeof(IOutboxChannelRegistry)))
        {
            return;
        }

        services.AddSingleton<IOutboxChannelRegistry>(sp =>
        {
            var registry = sp.GetRequiredService<OutboxChannelRegistry>();

            foreach (var configure in sp.GetServices<Action<OutboxChannelRegistry>>())
            {
                configure(registry);
            }

            return registry;
        });
    }
}
