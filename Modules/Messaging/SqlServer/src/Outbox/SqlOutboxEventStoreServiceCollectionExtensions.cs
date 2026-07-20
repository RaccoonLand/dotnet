using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaccoonLand.Modules.Messaging.Abstractions;

namespace RaccoonLand.Modules.Messaging.SqlServer;

/// <summary>
/// DI registration for the SQL Server <see cref="IOutboxEventStore"/> implementation.
/// </summary>
public static class SqlOutboxEventStoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="SqlOutboxEventStore"/> as <see cref="IOutboxEventStore"/> and binds
    /// <see cref="OutboxEventStoreOptions"/> + <see cref="SqlOutboxEventStoreOptions"/> from configuration.
    /// </summary>
    public static IServiceCollection AddRaccoonLandOutboxEventStore(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = OutboxEventStoreOptions.SectionName,
        Action<OutboxEventStoreOptions>? configureStore = null,
        Action<SqlOutboxEventStoreOptions>? configureSql = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        var section = configuration.GetSection(sectionName);

        var store = services.AddOptions<OutboxEventStoreOptions>().Bind(section);
        if (configureStore is not null)
        {
            store.Configure(configureStore);
        }

        store.Validate(
                options => SqlIdentifier.IsValid(options.Schema) && SqlIdentifier.IsValid(options.Table)
                    && (options.Database is null || SqlIdentifier.IsValid(options.Database)),
                "OutboxEventStore Database/Schema/Table must be simple SQL identifiers.")
            .ValidateOnStart();

        var sql = services.AddOptions<SqlOutboxEventStoreOptions>().Bind(section);
        if (configureSql is not null)
        {
            sql.Configure(configureSql);
        }

        sql.Validate(
                options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                "OutboxEventStore ConnectionString is required.")
            .ValidateOnStart();

        services.TryAddSingleton<SqlOutboxEventStoreConnectionFactory>();
        services.TryAddSingleton<IOutboxEventStore, SqlOutboxEventStore>();

        return services;
    }

    /// <summary>
    /// Registers <see cref="SqlOutboxEventStore"/> with code-only options (no <see cref="IConfiguration"/>).
    /// </summary>
    public static IServiceCollection AddRaccoonLandOutboxEventStore(
        this IServiceCollection services,
        Action<OutboxEventStoreOptions> configureStore,
        Action<SqlOutboxEventStoreOptions> configureSql)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureStore);
        ArgumentNullException.ThrowIfNull(configureSql);

        services.AddOptions<OutboxEventStoreOptions>()
            .Configure(configureStore)
            .Validate(
                options => SqlIdentifier.IsValid(options.Schema) && SqlIdentifier.IsValid(options.Table)
                    && (options.Database is null || SqlIdentifier.IsValid(options.Database)),
                "OutboxEventStore Database/Schema/Table must be simple SQL identifiers.")
            .ValidateOnStart();

        services.AddOptions<SqlOutboxEventStoreOptions>()
            .Configure(configureSql)
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                "OutboxEventStore ConnectionString is required.")
            .ValidateOnStart();

        services.TryAddSingleton<SqlOutboxEventStoreConnectionFactory>();
        services.TryAddSingleton<IOutboxEventStore, SqlOutboxEventStore>();

        return services;
    }
}
