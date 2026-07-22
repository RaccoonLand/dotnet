using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaccoonLand.Core.Hosting.AspNetCore.PipelineResponseMapping;
using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;

namespace RaccoonLand.Core.Hosting.AspNetCore.Tests.PipelineResponseMapping;

public sealed class DefaultPipelineResponseMapperTests
{
    private readonly DefaultPipelineResponseMapper _mapper = new();

    [Fact]
    public void Map_StatusHint_TakesPrecedenceOverErrors()
    {
        var response = new PipelineResponse
        {
            StatusHint = StatusCodes.Status403Forbidden,
            Errors = [new PipelineMessage("E", "denied")],
        };

        var result = Assert.IsType<ObjectResult>(_mapper.Map(response));

        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
        Assert.Same(response, result.Value);
    }

    [Fact]
    public void Map_FailureWithoutStatusHint_Returns400()
    {
        var response = new PipelineResponse
        {
            Errors = [new PipelineMessage("E", "bad")],
        };

        var result = Assert.IsType<ObjectResult>(_mapper.Map(response));

        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Same(response, result.Value);
    }

    [Fact]
    public void Map_Success_Returns200()
    {
        var response = new PipelineResponse { Result = 42 };

        var result = Assert.IsType<ObjectResult>(_mapper.Map(response));

        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.Equal(42, Assert.IsType<PipelineResponse>(result.Value).Result);
    }

    [Fact]
    public void Map_VoidEnvelope_Returns200()
    {
        var response = new PipelineResponse();

        var result = Assert.IsType<ObjectResult>(_mapper.Map(response));

        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.Null(Assert.IsType<PipelineResponse>(result.Value).Result);
    }

    [Fact]
    public void Map_NullResponse_Returns200WithEmptyEnvelope()
    {
        var result = Assert.IsType<ObjectResult>(_mapper.Map(null));

        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        var envelope = Assert.IsType<PipelineResponse>(result.Value);
        Assert.Null(envelope.Result);
        Assert.Empty(envelope.Errors);
        Assert.Empty(envelope.Warnings);
    }

    [Fact]
    public void Map_PreservesFullEnvelopeInBody()
    {
        var response = new PipelineResponse
        {
            Result = "payload",
            Errors = [new PipelineMessage("E1", "err")],
            Warnings = [new PipelineMessage("W1", "warn")],
            StatusHint = StatusCodes.Status409Conflict,
        };

        var result = Assert.IsType<ObjectResult>(_mapper.Map(response));
        var body = Assert.IsType<PipelineResponse>(result.Value);

        Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
        Assert.Equal("payload", body.Result);
        Assert.Equal("E1", Assert.Single(body.Errors).Code);
        Assert.Equal("W1", Assert.Single(body.Warnings).Code);
        Assert.Equal(StatusCodes.Status409Conflict, body.StatusHint);
    }
}
