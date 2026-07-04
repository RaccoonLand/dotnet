using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;

namespace RaccoonLand.Core.Hosting.Worker;

/// <summary>
/// Interprets a <see cref="PipelineResponse"/> after a background dispatch. Unlike the ASP.NET Core mapper,
/// this does not produce HTTP output — it decides success/failure for the worker loop (retry, ack, log, …).
/// The envelope itself is always returned unchanged on <see cref="WorkerDispatchResult.Response"/>.
/// </summary>
public interface IPipelineResponseHandler
{
    WorkerDispatchResult Handle(PipelineResponse? response);
}
