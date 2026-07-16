namespace RaccoonLand.Core.RequestProcessing.Abstractions.Responses;

/// <summary>
/// The default pipeline response envelope. Both successful and failed responses share this shape: on success
/// <see cref="Result"/> carries the payload and the message lists are empty; on a (domain) failure
/// <see cref="Result"/> is null and <see cref="Errors"/> describes what went wrong.
/// <para>
/// This is the type carried by <c>PipelineContext.Response</c>: the terminal handler wraps its result in a
/// <see cref="PipelineResponse"/>, and middleware may build one to short-circuit the pipeline.
/// </para>
/// </summary>
public sealed record PipelineResponse
{
    /// <summary>The actual payload on success; null when the request failed or produced no result.</summary>
    public object? Result { get; init; }

    /// <summary>Errors that prevented the request from succeeding. Empty on success.</summary>
    public IReadOnlyList<PipelineMessage> Errors { get; init; } = [];

    /// <summary>Non-fatal warnings produced while handling the request. Empty when there are none.</summary>
    public IReadOnlyList<PipelineMessage> Warnings { get; init; } = [];

    /// <summary>
    /// Optional status hint for the host adapter. This is <b>not</b> an HTTP API contract and does not
    /// require ASP.NET Core. Hosts may treat familiar HTTP-style numbers (for example 400, 401, 403, 404, 409)
    /// as a shared convention when mapping to their transport; other hosts (workers, message consumers) may
    /// ignore the hint or map it differently. When <see langword="null"/>, the host applies its own default.
    /// </summary>
    public int? StatusHint { get; init; }
}

/// <summary>
/// A single coded message: a stable <paramref name="Code"/> plus human-readable <paramref name="Message"/>
/// text supplied by the producer (endpoint, middleware, or validator). This type does not call
/// <c>IMessageLocalization</c> itself.
/// </summary>
public sealed record PipelineMessage(string Code, string Message);
