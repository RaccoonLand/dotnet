using RaccoonLand.Modules.Messaging.Abstractions;
using Xunit;

namespace RaccoonLand.Modules.Messaging.Abstractions.Tests.Outbox;

public sealed class QualifiedTableNameTests
{
    [Fact]
    public void OutboxOptions_WithDefaults_BuildsTwoPartBracketQuotedName()
    {
        var options = new OutboxEventStoreOptions();

        Assert.Equal("[dbo].[OutboxEvent]", options.QualifiedTableName);
    }

    [Fact]
    public void OutboxOptions_WithDatabase_BuildsThreePartName()
    {
        var options = new OutboxEventStoreOptions
        {
            Database = "AppDb",
            Schema = "messaging",
            Table = "Outbox",
        };

        Assert.Equal("[AppDb].[messaging].[Outbox]", options.QualifiedTableName);
    }

    [Fact]
    public void InboxOptions_WithDefaults_BuildsTwoPartBracketQuotedName()
    {
        var options = new InboxStoreOptions();

        Assert.Equal("[dbo].[InboxEvent]", options.QualifiedTableName);
    }

    [Fact]
    public void InboxOptions_WithDatabase_BuildsThreePartName()
    {
        var options = new InboxStoreOptions
        {
            Database = "AppDb",
            Schema = "messaging",
            Table = "Inbox",
        };

        Assert.Equal("[AppDb].[messaging].[Inbox]", options.QualifiedTableName);
    }

    [Theory]
    [InlineData("Outbox;DROP TABLE X")]
    [InlineData("Outbox]")]
    [InlineData("Outbox Event")]
    public void OutboxOptions_WithInjectionTable_Throws(string table)
    {
        var options = new OutboxEventStoreOptions { Table = table };

        Assert.Throws<ArgumentException>(() => _ = options.QualifiedTableName);
    }

    [Fact]
    public void InboxOptions_WithInjectionSchema_Throws()
    {
        var options = new InboxStoreOptions { Schema = "dbo];--" };

        Assert.Throws<ArgumentException>(() => _ = options.QualifiedTableName);
    }
}
