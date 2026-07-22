using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;
using RaccoonLand.Core.RequestProcessing.Abstractions.Results;

namespace RaccoonLand.Core.RequestProcessing.Abstractions.Tests.Results;

public sealed class ResultTests
{
    [Fact]
    public void Success_HasNoErrors_AndIsSuccess()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Success_WithWarnings_PreservesWarnings()
    {
        var warning = new PipelineMessage("W1", "careful");
        var result = Result.Success(warning);

        Assert.True(result.IsSuccess);
        Assert.Same(warning, Assert.Single(result.Warnings));
    }

    [Fact]
    public void Failure_SetsFailureFlags_AndPreservesErrors()
    {
        var error = new PipelineMessage("E1", "failed");
        var result = Result.Failure(error);

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Same(error, Assert.Single(result.Errors));
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Failure_Throws_WhenErrorsAreEmpty()
    {
        Assert.Throws<ArgumentException>(() => Result.Failure(Array.Empty<PipelineMessage>()));
    }

    [Fact]
    public void WithWarning_AppendsWarning()
    {
        var result = Result.Success().WithWarning(new PipelineMessage("W", "w"));

        Assert.True(result.IsSuccess);
        Assert.Equal("W", Assert.Single(result.Warnings).Code);
    }
}
