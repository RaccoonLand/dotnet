using RaccoonLand.Modules.Persistence.SqlServer.Commands.Outbox;

namespace RaccoonLand.Modules.Persistence.SqlServer.Commands.Tests.Outbox;

public sealed class SqlServerIdentifierTests
{
    [Theory]
    [InlineData("dbo")]
    [InlineData("messaging")]
    [InlineData("OutboxEvent")]
    [InlineData("_privateName")]
    [InlineData("A1_bc")]
    public void Validate_WhenValidRegularIdentifier_DoesNotThrow(string identifier)
    {
        SqlServerIdentifier.Validate(identifier, "Table");
    }

    [Fact]
    public void Validate_When128CharacterIdentifier_DoesNotThrow()
    {
        var maxLength = "a" + new string('b', 127);

        SqlServerIdentifier.Validate(maxLength, "Table");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenNullOrWhitespace_ThrowsArgumentException(string? identifier)
    {
        var ex = Assert.Throws<ArgumentException>(() => SqlServerIdentifier.Validate(identifier, "Table"));
        Assert.Equal("Table", ex.ParamName);
    }

    [Theory]
    [InlineData("1LeadingDigit")]
    [InlineData("has space")]
    [InlineData("has-dash")]
    [InlineData("has.dot")]
    [InlineData("has]bracket")]
    [InlineData("has;semicolon")]
    [InlineData("has'quote")]
    [InlineData("[dbo]")]
    [InlineData("\"quoted\"")]
    [InlineData("OutboxEvent]; DROP TABLE Users; --")]
    public void Validate_WhenInvalidCharsOrShape_ThrowsArgumentException(string identifier)
    {
        var ex = Assert.Throws<ArgumentException>(() => SqlServerIdentifier.Validate(identifier, "Table"));
        Assert.Equal("Table", ex.ParamName);
    }

    [Fact]
    public void Validate_WhenLongerThan128Characters_ThrowsArgumentException()
    {
        var tooLong = new string('a', 129);

        Assert.Throws<ArgumentException>(() => SqlServerIdentifier.Validate(tooLong, "Table"));
    }

    [Fact]
    public void QuotePart_WrapsIdentifierInBrackets()
    {
        Assert.Equal("[OutboxEvent]", SqlServerIdentifier.QuotePart("OutboxEvent"));
    }

    [Fact]
    public void QuotePart_DoublesEmbeddedClosingBracketAsDefenceInDepth()
    {
        Assert.Equal("[a]]b]", SqlServerIdentifier.QuotePart("a]b"));
    }
}
