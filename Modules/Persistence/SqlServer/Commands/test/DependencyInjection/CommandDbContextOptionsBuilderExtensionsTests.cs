using Microsoft.EntityFrameworkCore;
using RaccoonLand.Modules.Persistence.SqlServer.Commands.Outbox;

namespace RaccoonLand.Modules.Persistence.SqlServer.Commands.Tests.DependencyInjection;

public sealed class CommandDbContextOptionsBuilderExtensionsTests
{
    [Fact]
    public void AddRaccoonLandCommandInterceptors_WhenBuilderNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => CommandDbContextOptionsBuilderExtensions.AddRaccoonLandCommandInterceptors(
                null!, new OutboxOptions()));
    }

    [Fact]
    public void AddRaccoonLandCommandInterceptors_WhenOutboxOptionsNull_Throws()
    {
        var builder = new DbContextOptionsBuilder();

        Assert.Throws<ArgumentNullException>(
            () => builder.AddRaccoonLandCommandInterceptors(null!));
    }

    [Fact]
    public void AddRaccoonLandCommandInterceptors_WhenOutboxOptionsInvalid_ThrowsAtStartup()
    {
        // Empty Table was previously silent; it now surfaces at composition root as ArgumentException
        // instead of a hard-to-diagnose SQL syntax error on the first save.
        var builder = new DbContextOptionsBuilder();
        var options = new OutboxOptions { Table = "" };

        var ex = Assert.Throws<ArgumentException>(
            () => builder.AddRaccoonLandCommandInterceptors(options));
        Assert.Equal("Table", ex.ParamName);
    }

    [Fact]
    public void AddRaccoonLandCommandInterceptors_WhenOutboxOptionsSchemaInvalid_ThrowsAtStartup()
    {
        var builder = new DbContextOptionsBuilder();
        var options = new OutboxOptions { Schema = "bad schema", Table = "T" };

        var ex = Assert.Throws<ArgumentException>(
            () => builder.AddRaccoonLandCommandInterceptors(options));
        Assert.Equal("Schema", ex.ParamName);
    }

    [Fact]
    public void AddRaccoonLandCommandInterceptors_WhenValidOptions_DoesNotThrow()
    {
        var builder = new DbContextOptionsBuilder();
        var options = new OutboxOptions { Schema = "dbo", Table = "OutboxEvent" };

        builder.AddRaccoonLandCommandInterceptors(options);
    }
}
