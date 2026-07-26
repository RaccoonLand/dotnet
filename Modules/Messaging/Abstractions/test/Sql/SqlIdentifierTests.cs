using RaccoonLand.Modules.Messaging.Abstractions;
using Xunit;

namespace RaccoonLand.Modules.Messaging.Abstractions.Tests.Sql;

public sealed class SqlIdentifierTests
{
    [Theory]
    [InlineData("OutboxEvent")]
    [InlineData("dbo")]
    [InlineData("_underscore")]
    [InlineData("A1_b2_C3")]
    public void Require_WithSimpleIdentifier_ReturnsValue(string value)
    {
        Assert.Equal(value, SqlIdentifier.Require(value, "value"));
        Assert.True(SqlIdentifier.IsValid(value));
    }

    [Theory]
    [InlineData("Outbox;DROP TABLE Users")]
    [InlineData("Outbox Event")]
    [InlineData("Outbox]Event")]
    [InlineData("[Outbox]")]
    [InlineData("Outbox'Event")]
    [InlineData("Outbox-Event")]
    [InlineData("Outbox.Event")]
    [InlineData("1StartsWithDigit")]
    [InlineData("dbo.OutboxEvent")]
    public void Require_WithInjectionOrIllegalCharacters_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => SqlIdentifier.Require(value, "value"));
        Assert.False(SqlIdentifier.IsValid(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Require_WithNullOrWhitespace_Throws(string? value)
    {
        Assert.ThrowsAny<ArgumentException>(() => SqlIdentifier.Require(value, "value"));
        Assert.False(SqlIdentifier.IsValid(value));
    }

    [Fact]
    public void Require_WhenLongerThanMaxLength_Throws()
    {
        var tooLong = new string('a', 129);

        Assert.Throws<ArgumentException>(() => SqlIdentifier.Require(tooLong, "value"));
        Assert.False(SqlIdentifier.IsValid(tooLong));
        Assert.True(SqlIdentifier.IsValid(new string('a', 128)));
    }
}
