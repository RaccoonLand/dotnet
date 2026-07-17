using System.Globalization;
using Microsoft.Extensions.Options;
using RaccoonLand.Modules.MessageLocalization.Abstraction;
using RaccoonLand.Modules.MessageLocalization.SQLServer.Configuration;
using RaccoonLand.Modules.MessageLocalization.SQLServer.Storage;

namespace RaccoonLand.Modules.MessageLocalization.SQLServer;

/// <summary>
/// <see cref="IMessageLocalization"/> implementation backed by the in-memory store that is loaded from
/// SQL Server. Resolution never touches the database: lookups hit the snapshot and misses are recorded for
/// the background worker to persist.
/// </summary>
internal sealed class SqlServerMessageLocalization(
    MessageLocalizationStore store,
    MissingKeyTracker missingKeys,
    ICurrentCultureProvider cultureProvider,
    IOptions<MessageLocalizationSqlServerOptions> options) : IMessageLocalization
{
    private readonly MessageLocalizationStore _store = store;
    private readonly MissingKeyTracker _missingKeys = missingKeys;
    private readonly ICurrentCultureProvider _cultureProvider = cultureProvider;
    private readonly MessageLocalizationSqlServerOptions _options = options.Value;
    private readonly CultureInfo _defaultCulture = ResolveDefaultCulture(options.Value.DefaultCulture);

    /// <inheritdoc />
    public string Get(string messageTemplate, params object?[] parameters)
    {
        ArgumentNullException.ThrowIfNull(messageTemplate);
        ArgumentNullException.ThrowIfNull(parameters);
        return Format(ResolveCurrentCulture(), messageTemplate, parameters);
    }

    /// <inheritdoc />
    public string GetForCulture(CultureInfo culture, string messageTemplate, params object?[] parameters)
    {
        ArgumentNullException.ThrowIfNull(culture);
        ArgumentNullException.ThrowIfNull(messageTemplate);
        ArgumentNullException.ThrowIfNull(parameters);
        return Format(culture, messageTemplate, parameters);
    }

    private string Format(CultureInfo culture, string messageTemplate, object?[] parameters)
    {
        var template = Resolve(culture, messageTemplate);

        if (parameters.Length == 0)
        {
            return template;
        }

        var arguments = new object[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            arguments[i] = ResolveParameter(culture, parameters[i]);
        }

        try
        {
            return string.Format(culture, template, arguments);
        }
        catch (FormatException)
        {
            // The stored template does not match the supplied arguments; return it unformatted rather than throw.
            return template;
        }
    }

    private object ResolveParameter(CultureInfo culture, object? parameter) => parameter switch
    {
        // Literal string escape hatch (Raw is string-only; null Value → empty).
        RawValue raw => raw.Value ?? string.Empty,
        // By convention a string parameter is itself a localization key.
        string key => Resolve(culture, key),
        // Bare null element → empty literal.
        null => string.Empty,
        // Anything else (numbers, dates, ...) is a literal.
        _ => parameter,
    };

    private string Resolve(CultureInfo culture, string key)
    {
        foreach (var candidate in CultureChain(culture))
        {
            if (_store.TryGet(candidate, key, out var value))
            {
                return value;
            }
        }

        if (_options.AutoInsertMissingKeys)
        {
            _missingKeys.Report(NormalizeCultureName(culture), key);
        }

        // No translation yet: return the key itself so the caller still gets a meaningful, stable string.
        return key;
    }

    private IEnumerable<string> CultureChain(CultureInfo culture)
    {
        for (var current = culture; !string.IsNullOrEmpty(current.Name); current = current.Parent)
        {
            yield return current.Name;
        }

        if (!string.IsNullOrEmpty(_defaultCulture.Name)
            && !string.Equals(culture.Name, _defaultCulture.Name, StringComparison.OrdinalIgnoreCase))
        {
            yield return _defaultCulture.Name;
        }
    }

    private string NormalizeCultureName(CultureInfo culture)
        => string.IsNullOrEmpty(culture.Name) ? _defaultCulture.Name : culture.Name;

    // Get only: ICurrentCultureProvider (e.g. a request header) → configured default.
    private CultureInfo ResolveCurrentCulture()
        => _cultureProvider.GetCurrentCulture() ?? _defaultCulture;

    private static CultureInfo ResolveDefaultCulture(string name)
    {
        try
        {
            return CultureInfo.GetCultureInfo(name);
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.InvariantCulture;
        }
    }
}
