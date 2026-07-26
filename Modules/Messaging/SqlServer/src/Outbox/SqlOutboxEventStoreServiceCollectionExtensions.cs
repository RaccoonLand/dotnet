using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
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

        AttachStoreValidation(store);

        var sql = services.AddOptions<SqlOutboxEventStoreOptions>().Bind(section);
        if (configureSql is not null)
        {
            sql.Configure(configureSql);
        }

        AttachSqlValidation(sql);

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

        AttachStoreValidation(
            services.AddOptions<OutboxEventStoreOptions>().Configure(configureStore));

        AttachSqlValidation(
            services.AddOptions<SqlOutboxEventStoreOptions>().Configure(configureSql));

        services.TryAddSingleton<SqlOutboxEventStoreConnectionFactory>();
        services.TryAddSingleton<IOutboxEventStore, SqlOutboxEventStore>();

        return services;
    }

    private static void AttachStoreValidation(OptionsBuilder<OutboxEventStoreOptions> builder)
    {
        builder
            .Validate(
                static o => SqlIdentifier.IsValid(o.Schema),
                $"{OutboxEventStoreOptions.SectionName}.{nameof(OutboxEventStoreOptions.Schema)} must be a simple SQL identifier.")
            .Validate(
                static o => SqlIdentifier.IsValid(o.Table),
                $"{OutboxEventStoreOptions.SectionName}.{nameof(OutboxEventStoreOptions.Table)} must be a simple SQL identifier.")
            .Validate(
                static o => o.Database is null || SqlIdentifier.IsValid(o.Database),
                $"{OutboxEventStoreOptions.SectionName}.{nameof(OutboxEventStoreOptions.Database)} must be null or a simple SQL identifier.")
            .ValidateOnStart();
    }

    private static void AttachSqlValidation(OptionsBuilder<SqlOutboxEventStoreOptions> builder)
    {
        builder
            .Validate(
                static o => !string.IsNullOrWhiteSpace(o.ConnectionString),
                $"{OutboxEventStoreOptions.SectionName}.{nameof(SqlOutboxEventStoreOptions.ConnectionString)} is required.")
            .ValidateOnStart();
    }
}
