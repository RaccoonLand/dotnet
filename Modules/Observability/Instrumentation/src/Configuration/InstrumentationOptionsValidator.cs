using Microsoft.Extensions.Options;

namespace RaccoonLand.Modules.Observability.Instrumentation.Configuration;

/// <summary>
/// Rejects unsupported <see cref="InstrumentationOptions.RequestNameInMetrics"/> values at options
/// validation time (host startup when <c>ValidateOnStart</c> is used), so misconfiguration fails fast
/// without relying on per-request throws in the middleware.
/// </summary>
public sealed class InstrumentationOptionsValidator : IValidateOptions<InstrumentationOptions>
{
    public ValidateOptionsResult Validate(string? name, InstrumentationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (Enum.IsDefined(options.RequestNameInMetrics))
        {
            return ValidateOptionsResult.Success;
        }

        var allowed = string.Join(", ", Enum.GetNames<RequestNameMetricTag>());
        return ValidateOptionsResult.Fail(
            $"{nameof(InstrumentationOptions)}.{nameof(InstrumentationOptions.RequestNameInMetrics)} " +
            $"has unsupported value '{(int)options.RequestNameInMetrics}'. Allowed values: {allowed}.");
    }
}
