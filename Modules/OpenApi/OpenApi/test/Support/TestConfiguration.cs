using Microsoft.Extensions.Configuration;

namespace RaccoonLand.Modules.OpenApi.Tests.Support;

internal static class TestConfiguration
{
    public static IConfiguration FromDictionary(IDictionary<string, string?> values)
    {
        var builder = new ConfigurationBuilder();
        builder.Add(new DictionaryConfigurationSource(values));
        return builder.Build();
    }

    public static IConfiguration Empty() => FromDictionary(new Dictionary<string, string?>());

    private sealed class DictionaryConfigurationSource(IDictionary<string, string?> values) : IConfigurationSource
    {
        public IConfigurationProvider Build(IConfigurationBuilder builder)
            => new DictionaryConfigurationProvider(values);
    }

    private sealed class DictionaryConfigurationProvider : ConfigurationProvider
    {
        public DictionaryConfigurationProvider(IDictionary<string, string?> values)
        {
            Data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var (key, value) in values)
            {
                Data[key] = value;
            }
        }
    }
}
