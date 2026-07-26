using System.Globalization;
using Microsoft.Extensions.Logging;
using RaccoonLand.Modules.MessageLocalization.Abstraction;
using RaccoonLand.Modules.MessageLocalization.SQLServer;
using RaccoonLand.Modules.MessageLocalization.SQLServer.Storage;
using RaccoonLand.Modules.MessageLocalization.SQLServer.Tests.Support;
using static RaccoonLand.Modules.MessageLocalization.Abstraction.RawValue;

namespace RaccoonLand.Modules.MessageLocalization.SQLServer.Tests.Resolution;

public sealed class SqlServerMessageLocalizationTests
{
    [Fact]
    public void GetForCulture_WhenCultureNull_ThrowsArgumentNullException()
    {
        var localizer = LocalizationTestHelpers.CreateLocalizer(new MessageLocalizationStore());

        Assert.Throws<ArgumentNullException>(
            () => localizer.GetForCulture(null!, "KEY"));
    }

    [Fact]
    public void Get_WhenMessageTemplateNull_ThrowsArgumentNullException()
    {
        var localizer = LocalizationTestHelpers.CreateLocalizer(new MessageLocalizationStore());

        Assert.Throws<ArgumentNullException>(() => localizer.Get(null!));
    }

    [Fact]
    public void Get_WhenParametersNull_ThrowsArgumentNullException()
    {
        var localizer = LocalizationTestHelpers.CreateLocalizer(new MessageLocalizationStore());

        Assert.Throws<ArgumentNullException>(() => localizer.Get("KEY", null!));
    }

    [Fact]
    public void GetForCulture_WhenMessageTemplateNull_ThrowsArgumentNullException()
    {
        var localizer = LocalizationTestHelpers.CreateLocalizer(new MessageLocalizationStore());

        Assert.Throws<ArgumentNullException>(
            () => localizer.GetForCulture(CultureInfo.GetCultureInfo("en-US"), null!));
    }

    [Fact]
    public void GetForCulture_WhenParametersNull_ThrowsArgumentNullException()
    {
        var localizer = LocalizationTestHelpers.CreateLocalizer(new MessageLocalizationStore());

        Assert.Throws<ArgumentNullException>(
            () => localizer.GetForCulture(CultureInfo.GetCultureInfo("en-US"), "KEY", null!));
    }

    [Fact]
    public void Get_UsesCurrentCultureProvider_ThenFallsBackToDefaultCulture()
    {
        var store = new MessageLocalizationStore();
        store.Replace(
        [
            new LocalizationEntry("fr-FR", "Greeting", "Bonjour"),
            new LocalizationEntry("en-US", "Greeting", "Hello"),
        ]);

        var withProvider = LocalizationTestHelpers.CreateLocalizer(
            store,
            cultureProvider: new FixedCultureProvider(CultureInfo.GetCultureInfo("fr-FR")));
        Assert.Equal("Bonjour", withProvider.Get("Greeting"));

        var withoutProvider = LocalizationTestHelpers.CreateLocalizer(
            store,
            cultureProvider: NullCurrentCultureProvider.Instance,
            options: LocalizationTestHelpers.ValidOptions(o => o.DefaultCulture = "en-US"));
        Assert.Equal("Hello", withoutProvider.Get("Greeting"));
    }

    [Fact]
    public void GetForCulture_FallsBackThroughParentThenDefaultCulture()
    {
        var store = new MessageLocalizationStore();
        store.Replace(
        [
            new LocalizationEntry("fa", "Key", "از-پدر"),
            new LocalizationEntry("en-US", "Other", "fallback-default"),
        ]);

        var localizer = LocalizationTestHelpers.CreateLocalizer(
            store,
            options: LocalizationTestHelpers.ValidOptions(o => o.DefaultCulture = "en-US"));

        Assert.Equal("از-پدر", localizer.GetForCulture(CultureInfo.GetCultureInfo("fa-IR"), "Key"));
    }

    [Fact]
    public void GetForCulture_ResolvesParameters_RawStringNullAndLiteral()
    {
        var store = new MessageLocalizationStore();
        store.Replace(
        [
            new LocalizationEntry("en-US", "Welcome", "Hi {0}/{1}/{2}/{3}"),
            new LocalizationEntry("en-US", "ProductName", "Widget"),
        ]);

        var localizer = LocalizationTestHelpers.CreateLocalizer(store);
        var result = localizer.GetForCulture(
            CultureInfo.GetCultureInfo("en-US"),
            "Welcome",
            Raw("LiteralName"),
            "ProductName",
            null,
            42);

        Assert.Equal("Hi LiteralName/Widget//42", result);
    }

    [Fact]
    public void GetForCulture_FormatsWithProvidedCulture()
    {
        var store = new MessageLocalizationStore();
        store.Replace(
        [
            new LocalizationEntry("de-DE", "Amount", "Wert {0:N1}"),
        ]);

        var localizer = LocalizationTestHelpers.CreateLocalizer(store);
        var result = localizer.GetForCulture(CultureInfo.GetCultureInfo("de-DE"), "Amount", 12.5);

        Assert.Equal(
            string.Format(CultureInfo.GetCultureInfo("de-DE"), "Wert {0:N1}", 12.5),
            result);
    }

    [Fact]
    public void Get_WhenKeyMissing_ReturnsKeyAndReportsMissing()
    {
        var store = new MessageLocalizationStore();
        var tracker = new MissingKeyTracker();
        var localizer = LocalizationTestHelpers.CreateLocalizer(
            store,
            tracker,
            options: LocalizationTestHelpers.ValidOptions(o => o.AutoInsertMissingKeys = true));

        var result = localizer.Get("MISSING_KEY");

        Assert.Equal("MISSING_KEY", result);
        var drain = tracker.Drain();
        Assert.Contains(drain.Keys, k => k.Key == "MISSING_KEY" && k.Culture == "en-US");
        Assert.Equal(0, drain.DroppedSinceLastDrain);
    }

    [Fact]
    public void Get_WhenFormatMismatch_ReturnsUnformattedTemplate()
    {
        var store = new MessageLocalizationStore();
        store.Replace([new LocalizationEntry("en-US", "Bad", "Only {0} and {1}")]);

        var localizer = LocalizationTestHelpers.CreateLocalizer(store);
        Assert.Equal("Only {0} and {1}", localizer.Get("Bad", "one"));
    }

    [Fact]
    public void Resolve_UsesOnlyInMemoryStore()
    {
        var store = new MessageLocalizationStore();
        store.Replace([new LocalizationEntry("en-US", "FromStore", "store-value")]);

        var localizer = LocalizationTestHelpers.CreateLocalizer(store);

        Assert.Equal("store-value", localizer.Get("FromStore"));
        Assert.Equal("unknown", localizer.Get("unknown"));
    }

    [Fact]
    public void Constructor_WhenDefaultCultureInvalid_LogsWarningAndFallsBackToInvariant()
    {
        var logger = new ListLogger<SqlServerMessageLocalization>();

        // Construction must succeed even with a bogus culture name.
        _ = LocalizationTestHelpers.CreateLocalizer(
            new MessageLocalizationStore(),
            options: LocalizationTestHelpers.ValidOptions(o => o.DefaultCulture = "xx-BOGUS"),
            logger: logger);

        Assert.Contains(
            logger.Entries,
            e => e.Level == LogLevel.Warning
                 && e.Message.Contains("xx-BOGUS", StringComparison.Ordinal)
                 && e.Message.Contains("InvariantCulture", StringComparison.Ordinal));
    }

    [Fact]
    public void Constructor_WhenDefaultCultureValid_DoesNotLogWarning()
    {
        var logger = new ListLogger<SqlServerMessageLocalization>();

        _ = LocalizationTestHelpers.CreateLocalizer(
            new MessageLocalizationStore(),
            options: LocalizationTestHelpers.ValidOptions(o => o.DefaultCulture = "en-US"),
            logger: logger);

        Assert.DoesNotContain(logger.Entries, e => e.Level == LogLevel.Warning);
    }
}
