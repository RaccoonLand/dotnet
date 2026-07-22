using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;
using RaccoonLand.Core.RequestProcessing.Abstractions.Results;

namespace RaccoonLand.Core.RequestProcessing.Abstractions.Tests.Results;

public sealed class ResultExtensionsTests
{
    [Fact]
    public void ToPipelineResponse_FromResult_MapsErrorsAndWarnings_WithoutPayload()
    {
        var error = new PipelineMessage("E", "err");
        var warning = new PipelineMessage("W", "warn");
        var result = Result.Failure(error).WithWarning(warning);

        var response = result.ToPipelineResponse();

        Assert.Null(response.Result);
        Assert.Same(error, Assert.Single(response.Errors));
        Assert.Same(warning, Assert.Single(response.Warnings));
    }

    [Fact]
    public void ToPipelineResponse_FromResultOfT_Success_MapsPayload()
    {
        var warning = new PipelineMessage("W", "warn");
        var result = Result<int>.Success(99, warning);

        var response = result.ToPipelineResponse();

        Assert.Equal(99, response.Result);
        Assert.Empty(response.Errors);
        Assert.Same(warning, Assert.Single(response.Warnings));
    }

    [Fact]
    public void ToPipelineResponse_FromResultOfT_Failure_DoesNotMapPayload()
    {
        var error = new PipelineMessage("E", "err");
        var result = Result<int>.Failure(error);

        var response = result.ToPipelineResponse();

        Assert.Null(response.Result);
        Assert.Same(error, Assert.Single(response.Errors));
    }
}
