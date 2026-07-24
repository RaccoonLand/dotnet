using RaccoonLand.Modules.Persistence.SqlServer.Commands.Outbox;

namespace RaccoonLand.Modules.Persistence.SqlServer.Commands.Tests.Outbox;

public sealed class OutboxOptionsTests
{
    [Fact]
    public void QualifiedTableName_WhenNoDatabase_UsesTwoPartName()
    {
        var options = new OutboxOptions { Schema = "dbo", Table = "OutboxEvent" };

        Assert.Equal("[dbo].[OutboxEvent]", options.QualifiedTableName);
    }

    [Fact]
    public void QualifiedTableName_WhenDatabaseSet_UsesThreePartName()
    {
        var options = new OutboxOptions
        {
            Database = "SideEffects",
            Schema = "dbo",
            Table = "OutboxEvent",
        };

        Assert.Equal("[SideEffects].[dbo].[OutboxEvent]", options.QualifiedTableName);
    }

    [Fact]
    public void EnsureValid_WhenDefaults_DoesNotThrow()
    {
        var options = new OutboxOptions();

        options.EnsureValid();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("has space")]
    [InlineData("has-dash")]
    [InlineData("has.dot")]
    [InlineData("has]bracket")]
    public void EnsureValid_WhenTableInvalid_Throws(string table)
    {
        var options = new OutboxOptions { Table = table };

        var ex = Assert.Throws<ArgumentException>(options.EnsureValid);
        Assert.Equal("Table", ex.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("bad schema")]
    public void EnsureValid_WhenSchemaInvalid_Throws(string schema)
    {
        var options = new OutboxOptions { Schema = schema };

        var ex = Assert.Throws<ArgumentException>(options.EnsureValid);
        Assert.Equal("Schema", ex.ParamName);
    }

    [Fact]
    public void EnsureValid_WhenDatabaseSetToInvalidIdentifier_Throws()
    {
        var options = new OutboxOptions { Database = "bad db" };

        var ex = Assert.Throws<ArgumentException>(options.EnsureValid);
        Assert.Equal("Database", ex.ParamName);
    }

    [Fact]
    public void EnsureValid_WhenDatabaseNull_AllowsIt()
    {
        var options = new OutboxOptions { Database = null };

        options.EnsureValid();
    }
}
