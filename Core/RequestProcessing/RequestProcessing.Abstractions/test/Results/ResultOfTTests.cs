using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;
using RaccoonLand.Core.RequestProcessing.Abstractions.Results;

namespace RaccoonLand.Core.RequestProcessing.Abstractions.Tests.Results;

public sealed class ResultOfTTests
{
    [Fact]
    public void Success_PreservesPayload_AndIsSuccess()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(42, result.Value);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Success_WithWarnings_PreservesPayloadAndWarnings()
    {
        var warning = new PipelineMessage("W1", "note");
        var result = Result<string>.Success("ok", warning);

        Assert.Equal("ok", result.Value);
        Assert.Same(warning, Assert.Single(result.Warnings));
    }

    [Fact]
    public void Failure_ClearsPayload_AndPreservesErrors()
    {
        var error = new PipelineMessage("E1", "no");
        var result = Result<string>.Failure(error);

        Assert.True(result.IsFailure);
        Assert.Null(result.Value);
        Assert.Same(error, Assert.Single(result.Errors));
    }

    [Fact]
    public void Failure_Throws_WhenErrorsAreEmpty()
    {
        Assert.Throws<ArgumentException>(() => Result<int>.Failure(Array.Empty<PipelineMessage>()));
    }

    [Fact]
    public void ImplicitConversion_WrapsValueAsSuccess()
    {
        Result<int> result = 7;

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value);
    }
}
