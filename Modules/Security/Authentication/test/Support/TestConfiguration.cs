using Microsoft.Extensions.Configuration;

namespace RaccoonLand.Modules.Security.Authentication.Tests.Support;

internal static class TestConfiguration
{
    public static IConfiguration FromDictionary(
        IDictionary<string, string?> values,
        bool caseSensitiveKeys = false)
    {
        var builder = new ConfigurationBuilder();
        builder.Add(new DictionaryConfigurationSource(values, caseSensitiveKeys));
        return builder.Build();
    }

    private sealed class DictionaryConfigurationSource(
        IDictionary<string, string?> values,
        bool caseSensitiveKeys) : IConfigurationSource
    {
        public IConfigurationProvider Build(IConfigurationBuilder builder)
            => new DictionaryConfigurationProvider(values, caseSensitiveKeys);
    }

    private sealed class DictionaryConfigurationProvider : ConfigurationProvider
    {
        public DictionaryConfigurationProvider(IDictionary<string, string?> values, bool caseSensitiveKeys)
        {
            Data = new Dictionary<string, string?>(
                caseSensitiveKeys ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);

            foreach (var (key, value) in values)
            {
                Data[key] = value;
            }
        }
    }
}
