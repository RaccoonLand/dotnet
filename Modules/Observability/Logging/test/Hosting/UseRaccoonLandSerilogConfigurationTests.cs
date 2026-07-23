using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using RaccoonLand.Modules.Observability.Logging.Serilog.Hosting;
using RaccoonLand.Modules.Observability.Logging.Serilog.Tests.Support;

namespace RaccoonLand.Modules.Observability.Logging.Serilog.Tests.Hosting;

[Collection(SerilogHostCollection.Name)]
public sealed class UseRaccoonLandSerilogConfigurationTests
{
    [Fact]
    public void ExplicitConfiguration_BindsSection()
    {
        var configuration = SerilogTestHost.Configuration(
            ("RaccoonLandSerilog:ApplicationName", "FromConfig"),
            ("RaccoonLandSerilog:EnrichWithMachineName", "false"));

        var logEvent = SerilogTestHost.CaptureEvent(builder => builder.UseRaccoonLandSerilog(configuration));

        Assert.Equal("FromConfig", logEvent.Scalar("ApplicationName"));
        Assert.False(logEvent.HasProperty("MachineName"));
    }

    [Fact]
    public void ConfigureOptions_RunsAfterBind_AndOverridesConfiguration()
    {
        var configuration = SerilogTestHost.Configuration(
            ("RaccoonLandSerilog:ApplicationName", "FromConfig"));

        var logEvent = SerilogTestHost.CaptureEvent(builder =>
            builder.UseRaccoonLandSerilog(configuration, configureOptions: options => options.ApplicationName = "FromCode"));

        Assert.Equal("FromCode", logEvent.Scalar("ApplicationName"));
    }

    [Fact]
    public void NullConfiguration_UsesHostContextConfiguration()
    {
        var logEvent = SerilogTestHost.CaptureEvent(builder =>
        {
            builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["RaccoonLandSerilog:ApplicationName"] = "FromHostConfig" }));
            builder.UseRaccoonLandSerilog(configuration: null);
        });

        Assert.Equal("FromHostConfig", logEvent.Scalar("ApplicationName"));
    }

    [Fact]
    public void MissingSection_FallsBackToDefaultsWithoutError()
    {
        var logEvent = SerilogTestHost.CaptureEvent(builder =>
        {
            builder.ConfigureHostConfiguration(config => config.AddInMemoryCollection(
                new Dictionary<string, string?> { [HostDefaults.ApplicationKey] = "TestApp" }));
            builder.UseRaccoonLandSerilog(sectionName: "DoesNotExist");
        });

        // Defaults applied silently: ApplicationName falls back to the host app name and MachineName is on.
        Assert.Equal("TestApp", logEvent.Scalar("ApplicationName"));
        Assert.True(logEvent.HasProperty("MachineName"));
    }
}
