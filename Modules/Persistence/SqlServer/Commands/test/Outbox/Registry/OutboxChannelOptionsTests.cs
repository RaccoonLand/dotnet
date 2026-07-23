using RaccoonLand.Modules.Persistence.SqlServer.Commands.Outbox;

namespace RaccoonLand.Modules.Persistence.SqlServer.Commands.Tests.Outbox.Registry;

public sealed class OutboxChannelOptionsTests
{
    [Fact]
    public void QualifiedTableName_WhenNoDatabase_UsesTwoPartName()
    {
        var options = new OutboxChannelOptions { Schema = "messaging", Table = "QueueOutbox" };

        Assert.Equal("[messaging].[QueueOutbox]", options.QualifiedTableName);
    }

    [Fact]
    public void QualifiedTableName_WhenDatabaseSet_UsesThreePartName()
    {
        var options = new OutboxChannelOptions
        {
            Database = "SideEffects",
            Schema = "messaging",
            Table = "QueueOutbox",
        };

        Assert.Equal("[SideEffects].[messaging].[QueueOutbox]", options.QualifiedTableName);
    }

    [Fact]
    public void Schema_DefaultsToDbo()
    {
        var options = new OutboxChannelOptions { Table = "QueueOutbox" };

        Assert.Equal("[dbo].[QueueOutbox]", options.QualifiedTableName);
    }
}
