using Microsoft.Extensions.Hosting;
using RaccoonLand.Modules.Observability.Logging.Serilog.Hosting;

namespace RaccoonLand.Modules.Observability.Logging.Serilog.Tests.Hosting;

public sealed class UseRaccoonLandSerilogGuardTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void UseRaccoonLandSerilog_WhenSectionNameWhitespace_Throws(string sectionName)
    {
        var builder = new HostBuilder();

        Assert.Throws<ArgumentException>(() => builder.UseRaccoonLandSerilog(sectionName: sectionName));
    }

    [Fact]
    public void UseRaccoonLandSerilog_WhenHostBuilderNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => RaccoonLandSerilogHostBuilderExtensions.UseRaccoonLandSerilog(null!));
    }
}
