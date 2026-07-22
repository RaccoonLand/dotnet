using System.Globalization;
using RaccoonLand.Modules.MessageLocalization.Abstraction;

namespace RaccoonLand.Modules.MessageLocalization.SQLServer.Tests.Support;

internal sealed class FixedCultureProvider(CultureInfo? culture) : ICurrentCultureProvider
{
    public CultureInfo? GetCurrentCulture() => culture;
}
