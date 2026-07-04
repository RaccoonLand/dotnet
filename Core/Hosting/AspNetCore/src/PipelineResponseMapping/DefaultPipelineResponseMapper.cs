using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;

namespace RaccoonLand.Core.Hosting.AspNetCore.PipelineResponseMapping;

/// <summary>
/// Default mapping from <see cref="PipelineResponse"/> to HTTP. The <see cref="PipelineResponse"/> envelope is
/// always serialized as the response body; only the status code changes:
/// <list type="bullet">
///   <item><description><see cref="PipelineResponse.StatusHint"/> set → that value (for example 401/403 from the authorization middleware).</description></item>
///   <item><description>Otherwise, errors present → 400.</description></item>
///   <item><description>Otherwise → 200 (including void endpoints and <c>null</c> responses).</description></item>
/// </list>
/// </summary>
public sealed class DefaultPipelineResponseMapper : IPipelineResponseMapper
{
    public IActionResult Map(PipelineResponse? response)
    {
        var envelope = response ?? new PipelineResponse();
        var statusCode = envelope.StatusHint
            ?? (envelope.Errors.Count > 0
                ? StatusCodes.Status400BadRequest
                : StatusCodes.Status200OK);

        return new ObjectResult(envelope) { StatusCode = statusCode };
    }
}
