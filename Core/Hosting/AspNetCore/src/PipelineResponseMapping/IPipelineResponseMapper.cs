using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;

namespace RaccoonLand.Core.Hosting.AspNetCore.PipelineResponseMapping;

/// <summary>
/// Maps a <see cref="PipelineResponse"/> produced by the request pipeline to an ASP.NET Core
/// <see cref="Microsoft.AspNetCore.Mvc.IActionResult"/>. This is the seam between the host-agnostic pipeline
/// envelope and HTTP status codes / JSON bodies.
/// </summary>
public interface IPipelineResponseMapper
{
    /// <summary>Maps the pipeline envelope to an HTTP result.</summary>
    Microsoft.AspNetCore.Mvc.IActionResult Map(PipelineResponse? response);
}
