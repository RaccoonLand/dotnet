using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
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

        AttachStoreValidation(store);

        var sql = services.AddOptions<SqlInboxStoreOptions>().Bind(section);
        if (configureSql is not null)
        {
            sql.Configure(configureSql);
        }

        AttachSqlValidation(sql);

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

        AttachStoreValidation(
            services.AddOptions<InboxStoreOptions>().Configure(configureStore));

        AttachSqlValidation(
            services.AddOptions<SqlInboxStoreOptions>().Configure(configureSql));

        services.TryAddSingleton<SqlInboxStoreConnectionFactory>();
        services.TryAddSingleton<IInboxStore, SqlInboxStore>();

        return services;
    }

    private static void AttachStoreValidation(OptionsBuilder<InboxStoreOptions> builder)
    {
        builder
            .Validate(
                static o => SqlIdentifier.IsValid(o.Schema),
                $"{InboxStoreOptions.SectionName}.{nameof(InboxStoreOptions.Schema)} must be a simple SQL identifier.")
            .Validate(
                static o => SqlIdentifier.IsValid(o.Table),
                $"{InboxStoreOptions.SectionName}.{nameof(InboxStoreOptions.Table)} must be a simple SQL identifier.")
            .Validate(
                static o => o.Database is null || SqlIdentifier.IsValid(o.Database),
                $"{InboxStoreOptions.SectionName}.{nameof(InboxStoreOptions.Database)} must be null or a simple SQL identifier.")
            .ValidateOnStart();
    }

    private static void AttachSqlValidation(OptionsBuilder<SqlInboxStoreOptions> builder)
    {
        builder
            .Validate(
                static o => !string.IsNullOrWhiteSpace(o.ConnectionString),
                $"{InboxStoreOptions.SectionName}.{nameof(SqlInboxStoreOptions.ConnectionString)} is required.")
            .ValidateOnStart();
    }
}
