using RaccoonLand.Modules.Observability.Logging.Serilog.Configuration;

namespace RaccoonLand.Modules.Observability.Logging.Serilog.Tests.Configuration;

public sealed class RaccoonLandSerilogOptionsTests
{
    [Fact]
    public void SectionName_IsRaccoonLandSerilog()
    {
        Assert.Equal("RaccoonLandSerilog", RaccoonLandSerilogOptions.SectionName);
    }

    [Fact]
    public void ApplicationName_DefaultsToEmpty()
    {
        Assert.Equal(string.Empty, new RaccoonLandSerilogOptions().ApplicationName);
    }

    [Fact]
    public void EnrichmentToggles_DefaultToTrue()
    {
        var options = new RaccoonLandSerilogOptions();

        Assert.True(options.EnrichWithApplicationName);
        Assert.True(options.EnrichWithEnvironmentName);
        Assert.True(options.EnrichWithMachineName);
        Assert.True(options.EnrichWithTraceId);
    }
}
