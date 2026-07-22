using System.Globalization;
using RaccoonLand.Modules.MessageLocalization.Abstraction;

namespace RaccoonLand.Modules.Middlewares.ExceptionHandlingMiddleware.Tests.Support;

internal sealed class FakeMessageLocalization : IMessageLocalization
{
    public List<(string Template, object?[] Parameters)> Calls { get; } = [];

    public string Get(string messageTemplate, params object?[] parameters)
    {
        Calls.Add((messageTemplate, parameters));
        var args = string.Join(",", parameters.Select(p => p?.ToString() ?? string.Empty));
        return $"LOC:{messageTemplate}:{args}";
    }

    public string GetForCulture(CultureInfo culture, string messageTemplate, params object?[] parameters)
        => Get(messageTemplate, parameters);
}
