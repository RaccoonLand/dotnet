using Microsoft.Extensions.Configuration;
using RaccoonLand.Modules.Security.Authentication.Configuration;

namespace RaccoonLand.Modules.Security.Authentication;

internal static class AuthenticationSchemeOptionsBinder
{
    public static void MergeJwtBearerSchemes(
        IConfigurationSection schemesSection,
        Dictionary<string, JwtBearerOptions> schemes)
    {
        MergeSchemes(schemesSection, schemes, static () => new JwtBearerOptions());
    }

    public static void MergeOpenIdConnectSchemes(
        IConfigurationSection schemesSection,
        Dictionary<string, OpenIdConnectOptions> schemes)
    {
        MergeSchemes(schemesSection, schemes, static () => new OpenIdConnectOptions());
    }

    private static void MergeSchemes<TOptions>(
        IConfigurationSection schemesSection,
        Dictionary<string, TOptions> schemes,
        Func<TOptions> factory)
        where TOptions : class
    {
        foreach (var schemeSection in schemesSection.GetChildren())
        {
            if (!schemes.TryGetValue(schemeSection.Key, out var schemeOptions))
            {
                schemeOptions = factory();
                schemes[schemeSection.Key] = schemeOptions;
            }

            schemeSection.Bind(schemeOptions);
        }
    }
}
