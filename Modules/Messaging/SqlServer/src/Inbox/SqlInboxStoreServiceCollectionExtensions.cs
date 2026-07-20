using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaccoonLand.Modules.Messaging.Abstractions;

namespace RaccoonLand.Modules.Messaging.SqlServer;

/// <summary>
/// DI registration for the SQL Server <see cref="IInboxStore"/> implementation.
/// </summary>
public static class SqlInboxStoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="SqlInboxStore"/> as <see cref="IInboxStore"/> and binds inbox options
    /// from configuration (section <see cref="InboxStoreOptions.SectionName"/> by default).
    /// </summary>
    public static IServiceCollection AddRaccoonLandInboxStore(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = InboxStoreOptions.SectionName,
        Action<InboxStoreOptions>? configureStore = null,
        Action<SqlInboxStoreOptions>? configureSql = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        var section = configuration.GetSection(sectionName);

        var store = services.AddOptions<InboxStoreOptions>().Bind(section);
        if (configureStore is not null)
        {
            store.Configure(configureStore);
        }

        store.Validate(
                options => SqlIdentifier.IsValid(options.Schema) && SqlIdentifier.IsValid(options.Table)
                    && (options.Database is null || SqlIdentifier.IsValid(options.Database)),
                "InboxStore Database/Schema/Table must be simple SQL identifiers.")
            .ValidateOnStart();

        var sql = services.AddOptions<SqlInboxStoreOptions>().Bind(section);
        if (configureSql is not null)
        {
            sql.Configure(configureSql);
        }

        sql.Validate(
                options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                "InboxStore ConnectionString is required.")
            .ValidateOnStart();

        services.TryAddSingleton<SqlInboxStoreConnectionFactory>();
        services.TryAddSingleton<IInboxStore, SqlInboxStore>();

        return services;
    }

    /// <summary>
    /// Registers <see cref="SqlInboxStore"/> with code-only options.
    /// </summary>
    public static IServiceCollection AddRaccoonLandInboxStore(
        this IServiceCollection services,
        Action<InboxStoreOptions> configureStore,
        Action<SqlInboxStoreOptions> configureSql)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureStore);
        ArgumentNullException.ThrowIfNull(configureSql);

        services.AddOptions<InboxStoreOptions>()
            .Configure(configureStore)
            .Validate(
                options => SqlIdentifier.IsValid(options.Schema) && SqlIdentifier.IsValid(options.Table)
                    && (options.Database is null || SqlIdentifier.IsValid(options.Database)),
                "InboxStore Database/Schema/Table must be simple SQL identifiers.")
            .ValidateOnStart();

        services.AddOptions<SqlInboxStoreOptions>()
            .Configure(configureSql)
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                "InboxStore ConnectionString is required.")
            .ValidateOnStart();

        services.TryAddSingleton<SqlInboxStoreConnectionFactory>();
        services.TryAddSingleton<IInboxStore, SqlInboxStore>();

        return services;
    }
}
