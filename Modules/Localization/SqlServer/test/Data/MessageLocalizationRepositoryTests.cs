using System.Reflection;
using RaccoonLand.Modules.MessageLocalization.SQLServer.Data;
using RaccoonLand.Modules.MessageLocalization.SQLServer.Tests.Support;

namespace RaccoonLand.Modules.MessageLocalization.SQLServer.Tests.Data;

public sealed class MessageLocalizationRepositoryTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1StartsWithDigit")]
    [InlineData("bad-name")]
    public void Constructor_WhenSchemaIdentifierInvalid_Throws(string schema)
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => LocalizationTestHelpers.CreateRepository(o => o.SchemaName = schema));

        Assert.Contains("SchemaName", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_WhenIdentifierTooLong_Throws()
    {
        var tooLong = new string('A', 129);

        var ex = Assert.Throws<InvalidOperationException>(
            () => LocalizationTestHelpers.CreateRepository(o => o.ServicesTableName = tooLong));

        Assert.Contains("ServicesTableName", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_BracketQuotesQualifiedTableNames()
    {
        var repository = LocalizationTestHelpers.CreateRepository(o =>
        {
            o.SchemaName = "Loc";
            o.ServicesTableName = "Svc";
            o.ApplicationsTableName = "Apps";
            o.MessageLocalizationsTableName = "Msgs";
        });

        Assert.Equal("[Loc].[Svc]", GetPrivateString(repository, "_servicesTable"));
        Assert.Equal("[Loc].[Apps]", GetPrivateString(repository, "_applicationsTable"));
        Assert.Equal("[Loc].[Msgs]", GetPrivateString(repository, "_messagesTable"));
    }

    [Fact]
    public void Constructor_EscapesClosingBracketInIdentifier()
    {
        // Valid regex allows letters only for typical names; Quote still escapes ].
        // Use reflection on Quote via a valid identifier path by calling Qualify indirectly:
        // identifiers with ] fail ValidateIdentifier, so assert Quote behavior through private method.
        var quote = typeof(MessageLocalizationRepository)
            .GetMethod("Quote", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(quote);

        var quoted = (string)quote.Invoke(null, ["Name]Weird"])!;
        Assert.Equal("[Name]]Weird]", quoted);
    }

    private static string GetPrivateString(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return Assert.IsType<string>(field.GetValue(instance));
    }
}
