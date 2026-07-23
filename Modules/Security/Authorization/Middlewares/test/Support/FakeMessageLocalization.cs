using System.Globalization;
using RaccoonLand.Modules.MessageLocalization.Abstraction;

namespace RaccoonLand.Modules.Security.Authorization.Middlewares.Tests.Support;

/// <summary>
/// Minimal <see cref="IMessageLocalization"/> that returns a deterministic "LOC:{template}" string and records
/// the templates it was asked to resolve.
/// </summary>
internal sealed class FakeMessageLocalization : IMessageLocalization
{
    public List<string> ResolvedTemplates { get; } = [];

    public string Get(string messageTemplate, params object?[] parameters)
    {
        ResolvedTemplates.Add(messageTemplate);
        return "LOC:" + messageTemplate;
    }

    public string GetForCulture(CultureInfo culture, string messageTemplate, params object?[] parameters)
        => Get(messageTemplate, parameters);
}
