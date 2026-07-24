using RaccoonLand.Modules.Persistence.SqlServer.Commands.Outbox;
using RaccoonLand.Modules.Persistence.SqlServer.Commands.Tests.Support;

namespace RaccoonLand.Modules.Persistence.SqlServer.Commands.Tests.Outbox.Registry;

public sealed class OutboxChannelRegistryTests
{
    [Fact]
    public void Register_WhenOptionsNull_ThrowsArgumentNullException()
    {
        var registry = new OutboxChannelRegistry();

        Assert.Throws<ArgumentNullException>(() => registry.Register<ITestOutbox>(null!));
    }

    [Fact]
    public void Register_WhenTypeDoesNotImplementIOutbox_ThrowsArgumentException()
    {
        var registry = new OutboxChannelRegistry();

        Assert.Throws<ArgumentException>(
            () => registry.Register(typeof(string), new OutboxChannelOptions { Table = "T" }));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_WhenTableEmptyOrWhitespace_ThrowsArgumentException(string table)
    {
        var registry = new OutboxChannelRegistry();

        Assert.Throws<ArgumentException>(
            () => registry.Register<ITestOutbox>(new OutboxChannelOptions { Table = table }));
    }

    [Theory]
    [InlineData("has space")]
    [InlineData("has-dash")]
    [InlineData("has.dot")]
    [InlineData("has]bracket")]
    [InlineData("OutboxEvent]; DROP TABLE Users; --")]
    public void Register_WhenTableHasUnsafeChars_ThrowsArgumentException(string table)
    {
        var registry = new OutboxChannelRegistry();

        var ex = Assert.Throws<ArgumentException>(
            () => registry.Register<ITestOutbox>(new OutboxChannelOptions { Table = table }));
        Assert.Equal("Table", ex.ParamName);
    }

    [Fact]
    public void Register_WhenSchemaInvalid_ThrowsArgumentException()
    {
        var registry = new OutboxChannelRegistry();

        var ex = Assert.Throws<ArgumentException>(
            () => registry.Register<ITestOutbox>(new OutboxChannelOptions
            {
                Schema = "bad schema",
                Table = "T",
            }));
        Assert.Equal("Schema", ex.ParamName);
    }

    [Fact]
    public void Register_WhenDatabaseSetToInvalidIdentifier_ThrowsArgumentException()
    {
        var registry = new OutboxChannelRegistry();

        var ex = Assert.Throws<ArgumentException>(
            () => registry.Register<ITestOutbox>(new OutboxChannelOptions
            {
                Database = "bad db",
                Table = "T",
            }));
        Assert.Equal("Database", ex.ParamName);
    }

    [Fact]
    public void Register_WhenSameChannelRegisteredTwice_LastRegistrationWins()
    {
        var registry = new OutboxChannelRegistry();
        registry.Register<ITestOutbox>(new OutboxChannelOptions { Table = "First" });
        registry.Register<ITestOutbox>(new OutboxChannelOptions { Table = "Second" });

        Assert.Equal("Second", registry.Get<ITestOutbox>()!.Table);
    }

    [Fact]
    public void Get_WhenChannelNotRegistered_ReturnsNull()
    {
        var registry = new OutboxChannelRegistry();

        Assert.Null(registry.Get<ITestOutbox>());
        Assert.Null(registry.Get(typeof(ITestOutbox)));
    }

    [Fact]
    public void Channels_ReturnsAllRegisteredMarkerTypes()
    {
        var registry = new OutboxChannelRegistry();
        registry.Register<ITestOutbox>(new OutboxChannelOptions { Table = "T" });
        registry.Register<IOtherOutbox>(new OutboxChannelOptions { Table = "O" });

        Assert.Contains(typeof(ITestOutbox), registry.Channels);
        Assert.Contains(typeof(IOtherOutbox), registry.Channels);
        Assert.Equal(2, registry.Channels.Count);
    }
}
