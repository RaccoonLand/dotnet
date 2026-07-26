using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RaccoonLand.Modules.MessageLocalization.Abstraction;
using RaccoonLand.Modules.MessageLocalization.SQLServer;
using RaccoonLand.Modules.MessageLocalization.SQLServer.Configuration;
using RaccoonLand.Modules.MessageLocalization.SQLServer.Data;
using RaccoonLand.Modules.MessageLocalization.SQLServer.Hosting;
using RaccoonLand.Modules.MessageLocalization.SQLServer.Storage;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registration helpers for the SQL Server message-localization implementation.
/// </summary>
public static class MessageLocalizationSqlServerServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IMessageLocalization"/> backed by SQL Server, binding options from the given
    /// configuration <paramref name="sectionName"/> (defaults to <c>MessageLocalization</c>).
    /// </summary>
    public static IServiceCollection AddRaccoonLandMessageLocalizationSqlServer(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = MessageLocalizationSqlServerOptions.SectionName)
    {
        AttachValidation(
            services.AddOptions<MessageLocalizationSqlServerOptions>()
                .Bind(configuration.GetSection(sectionName)));

        return services.AddCore();
    }

    /// <summary>
    /// Registers <see cref="IMessageLocalization"/> backed by SQL Server, configuring options in code.
    /// </summary>
    public static IServiceCollection AddRaccoonLandMessageLocalizationSqlServer(
        this IServiceCollection services,
        Action<MessageLocalizationSqlServerOptions> configure)
    {
        AttachValidation(
            services.AddOptions<MessageLocalizationSqlServerOptions>()
                .Configure(configure));

        return services.AddCore();
    }

    private static IServiceCollection AddCore(this IServiceCollection services)
    {
        services.TryAddSingleton<MessageLocalizationStore>();
        services.TryAddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MessageLocalizationSqlServerOptions>>().Value;
            return new MissingKeyTracker(options.MaxPendingMissingKeys);
        });
        services.TryAddSingleton<MessageLocalizationRepository>();
        services.TryAddSingleton<IMessageLocalizationRepository>(sp =>
            sp.GetRequiredService<MessageLocalizationRepository>());
        // Default culture provider; a consumer can override it by registering its own ICurrentCultureProvider.
        services.TryAddSingleton<ICurrentCultureProvider, NullCurrentCultureProvider>();
        services.TryAddSingleton<IMessageLocalization, SqlServerMessageLocalization>();
        services.AddHostedService<MessageLocalizationRefreshService>();

        return services;
    }

    private static void AttachValidation(OptionsBuilder<MessageLocalizationSqlServerOptions> builder)
    {
        builder
            .Validate(
                static o => !string.IsNullOrWhiteSpace(o.ConnectionString),
                $"{MessageLocalizationSqlServerOptions.SectionName}.{nameof(MessageLocalizationSqlServerOptions.ConnectionString)} is required.")
            .Validate(
                static o => !string.IsNullOrWhiteSpace(o.ServiceName),
                $"{MessageLocalizationSqlServerOptions.SectionName}.{nameof(MessageLocalizationSqlServerOptions.ServiceName)} is required.")
            .Validate(
                static o => !string.IsNullOrWhiteSpace(o.ApplicationName),
                $"{MessageLocalizationSqlServerOptions.SectionName}.{nameof(MessageLocalizationSqlServerOptions.ApplicationName)} is required.")
            .Validate(
                static o => o.MaxPendingMissingKeys > 0,
                $"{MessageLocalizationSqlServerOptions.SectionName}.{nameof(MessageLocalizationSqlServerOptions.MaxPendingMissingKeys)} must be greater than zero.")
            .ValidateOnStart();
    }
}
