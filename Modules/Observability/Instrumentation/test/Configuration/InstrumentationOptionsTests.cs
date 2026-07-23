using RaccoonLand.Modules.Observability.Instrumentation.Configuration;

namespace RaccoonLand.Modules.Observability.Instrumentation.Tests.Configuration;

public sealed class InstrumentationOptionsTests
{
    [Fact]
    public void Defaults_EnableAllPillars()
    {
        var options = new InstrumentationOptions();

        Assert.True(options.EnableTracing);
        Assert.True(options.EnableMetrics);
        Assert.True(options.EnableLogging);
    }

    [Fact]
    public void Defaults_RequestNameInMetricsIsFullName()
    {
        var options = new InstrumentationOptions();

        Assert.Equal(RequestNameMetricTag.FullName, options.RequestNameInMetrics);
    }

    [Fact]
    public void SectionName_IsObservabilityInstrumentation()
    {
        Assert.Equal("Observability:Instrumentation", InstrumentationOptions.SectionName);
    }
}
