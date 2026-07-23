using Microsoft.Extensions.Options;
using RaccoonLand.Modules.Observability.Instrumentation.Configuration;

namespace RaccoonLand.Modules.Observability.Instrumentation.Tests.Configuration;

public sealed class InstrumentationOptionsValidatorTests
{
    private readonly InstrumentationOptionsValidator _validator = new();

    [Theory]
    [InlineData(RequestNameMetricTag.None)]
    [InlineData(RequestNameMetricTag.Name)]
    [InlineData(RequestNameMetricTag.FullName)]
    public void Validate_WhenRequestNameInMetricsDefined_Succeeds(RequestNameMetricTag tag)
    {
        var options = new InstrumentationOptions { RequestNameInMetrics = tag };

        var result = _validator.Validate(name: null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_WhenRequestNameInMetricsUndefined_Fails()
    {
        var options = new InstrumentationOptions { RequestNameInMetrics = (RequestNameMetricTag)999 };

        var result = _validator.Validate(name: null, options);

        Assert.True(result.Failed);
        Assert.Contains("unsupported value", result.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_WhenOptionsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _validator.Validate(name: null, options: null!));
    }
}
