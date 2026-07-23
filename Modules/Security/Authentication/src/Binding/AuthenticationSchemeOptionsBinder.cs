using Microsoft.Extensions.Configuration;
using RaccoonLand.Modules.Security.Authentication.Configuration;

namespace RaccoonLand.Modules.Security.Authentication.Binding;

internal static class AuthenticationSchemeOptionsBinder
{
    public static void MergeJwtBearerSchemes(
        IConfigurationSection schemesSection,
        Dictionary<string, JwtBearerOptions> schemes)
    {
        MergeSchemes(
            schemesSection,
            schemes,
            nameof(AuthenticationOptions.JwtBearer),
            static () => new JwtBearerOptions());
    }

    public static void MergeOpenIdConnectSchemes(
        IConfigurationSection schemesSection,
        Dictionary<string, OpenIdConnectOptions> schemes)
    {
        MergeSchemes(
            schemesSection,
            schemes,
            nameof(AuthenticationOptions.OpenIdConnect),
            static () => new OpenIdConnectOptions());
    }

    private static void MergeSchemes<TOptions>(
        IConfigurationSection schemesSection,
        Dictionary<string, TOptions> schemes,
        string dictionaryName,
        Func<TOptions> factory)
        where TOptions : class
    {
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var schemeSection in schemesSection.GetChildren())
        {
            if (string.IsNullOrWhiteSpace(schemeSection.Key))
            {
                throw new InvalidOperationException(
                    $"Authentication scheme name under '{dictionaryName}' cannot be null, empty, or whitespace.");
            }

            if (!seenKeys.Add(schemeSection.Key))
            {
                throw new InvalidOperationException(
                    $"Duplicate authentication scheme name under '{dictionaryName}' (case-insensitive): '{schemeSection.Key}'.");
            }

            if (!schemes.TryGetValue(schemeSection.Key, out var schemeOptions))
            {
                schemeOptions = factory();
                schemes[schemeSection.Key] = schemeOptions;
            }

            schemeSection.Bind(schemeOptions);
        }
    }
}
