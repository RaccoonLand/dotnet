using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using RaccoonLand.Modules.Observability.Logging.Serilog.Hosting;
using RaccoonLand.Modules.Observability.Logging.Serilog.Tests.Support;

namespace RaccoonLand.Modules.Observability.Logging.Serilog.Tests.Hosting;

[Collection(SerilogHostCollection.Name)]
public sealed class RaccoonLandSerilogEnrichmentTests
{
    [Fact]
    public void ApplicationName_WhenOptionEmpty_FallsBackToHostEnvironment()
    {
        var logEvent = SerilogTestHost.CaptureEvent(builder =>
        {
            WithApplicationName(builder, "TestApp");
            builder.UseRaccoonLandSerilog();
        });

        Assert.Equal("TestApp", logEvent.Scalar("ApplicationName"));
    }

    [Fact]
    public void ApplicationName_WhenOptionSet_UsesConfiguredValue()
    {
        var logEvent = SerilogTestHost.CaptureEvent(builder =>
        {
            WithApplicationName(builder, "HostApp");
            builder.UseRaccoonLandSerilog(configureOptions: options => options.ApplicationName = "ExplicitApp");
        });

        Assert.Equal("ExplicitApp", logEvent.Scalar("ApplicationName"));
    }

    [Fact]
    public void EnvironmentName_IsEnrichedFromHostEnvironment()
    {
        var logEvent = SerilogTestHost.CaptureEvent(builder =>
        {
            builder.UseEnvironment("Staging");
            builder.UseRaccoonLandSerilog();
        });

        Assert.Equal("Staging", logEvent.Scalar("EnvironmentName"));
    }

    [Fact]
    public void MachineName_IsEnrichedFromEnvironment()
    {
        var logEvent = SerilogTestHost.CaptureEvent(builder => builder.UseRaccoonLandSerilog());

        Assert.Equal(Environment.MachineName, logEvent.Scalar("MachineName"));
    }

    [Fact]
    public void TraceId_IsEnrichedWhenActivityActive()
    {
        using var activity = new Activity("test").Start();

        var logEvent = SerilogTestHost.CaptureEvent(builder => builder.UseRaccoonLandSerilog());

        Assert.Equal(activity.TraceId.ToString(), logEvent.Scalar("TraceId"));
    }

    [Fact]
    public void EnrichWithApplicationName_WhenFalse_OmitsProperty()
    {
        var logEvent = SerilogTestHost.CaptureEvent(builder =>
            builder.UseRaccoonLandSerilog(configureOptions: options => options.EnrichWithApplicationName = false));

        Assert.False(logEvent.HasProperty("ApplicationName"));
    }

    [Fact]
    public void EnrichWithEnvironmentName_WhenFalse_OmitsProperty()
    {
        var logEvent = SerilogTestHost.CaptureEvent(builder =>
            builder.UseRaccoonLandSerilog(configureOptions: options => options.EnrichWithEnvironmentName = false));

        Assert.False(logEvent.HasProperty("EnvironmentName"));
    }

    [Fact]
    public void EnrichWithMachineName_WhenFalse_OmitsProperty()
    {
        var logEvent = SerilogTestHost.CaptureEvent(builder =>
            builder.UseRaccoonLandSerilog(configureOptions: options => options.EnrichWithMachineName = false));

        Assert.False(logEvent.HasProperty("MachineName"));
    }

    [Fact]
    public void EnrichWithTraceId_WhenFalse_OmitsPropertyEvenWithActivity()
    {
        using var activity = new Activity("test").Start();

        var logEvent = SerilogTestHost.CaptureEvent(builder =>
            builder.UseRaccoonLandSerilog(configureOptions: options => options.EnrichWithTraceId = false));

        Assert.False(logEvent.HasProperty("TraceId"));
    }

    private static void WithApplicationName(IHostBuilder builder, string applicationName)
        => builder.ConfigureHostConfiguration(config => config.AddInMemoryCollection(
            new Dictionary<string, string?> { [HostDefaults.ApplicationKey] = applicationName }));
}
