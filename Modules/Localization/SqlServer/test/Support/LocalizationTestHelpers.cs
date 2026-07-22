using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RaccoonLand.Modules.MessageLocalization.Abstraction;
using RaccoonLand.Modules.MessageLocalization.SQLServer.Configuration;
using RaccoonLand.Modules.MessageLocalization.SQLServer.Data;
using RaccoonLand.Modules.MessageLocalization.SQLServer.Hosting;
using RaccoonLand.Modules.MessageLocalization.SQLServer.Storage;

namespace RaccoonLand.Modules.MessageLocalization.SQLServer.Tests.Support;

internal static class LocalizationTestHelpers
{
    public static MessageLocalizationSqlServerOptions ValidOptions(
        Action<MessageLocalizationSqlServerOptions>? configure = null)
    {
        var options = new MessageLocalizationSqlServerOptions
        {
            ConnectionString = "Server=.;Database=Test;Trusted_Connection=True;",
            ServiceName = "Ordering",
            ApplicationName = "Ordering.Api",
        };
        configure?.Invoke(options);
        return options;
    }

    public static SqlServerMessageLocalization CreateLocalizer(
        MessageLocalizationStore store,
        MissingKeyTracker? missingKeys = null,
        ICurrentCultureProvider? cultureProvider = null,
        MessageLocalizationSqlServerOptions? options = null)
    {
        return new SqlServerMessageLocalization(
            store,
            missingKeys ?? new MissingKeyTracker(),
            cultureProvider ?? NullCurrentCultureProvider.Instance,
            Options.Create(options ?? ValidOptions()));
    }

    public static MessageLocalizationRefreshService CreateRefreshService(
        FakeMessageLocalizationRepository repository,
        MessageLocalizationStore? store = null,
        MissingKeyTracker? missingKeys = null,
        MessageLocalizationSqlServerOptions? options = null)
    {
        return new MessageLocalizationRefreshService(
            repository,
            store ?? new MessageLocalizationStore(),
            missingKeys ?? new MissingKeyTracker(),
            Options.Create(options ?? ValidOptions()),
            NullLogger<MessageLocalizationRefreshService>.Instance);
    }

    public static MessageLocalizationRepository CreateRepository(
        Action<MessageLocalizationSqlServerOptions>? configure = null)
        => new(Options.Create(ValidOptions(configure)));
}
