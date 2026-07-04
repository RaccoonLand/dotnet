using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;

namespace RaccoonLand.Core.RequestProcessing.Abstractions.Results;

/// <summary>
/// The outcome of an endpoint that produces no payload. Carries the domain <see cref="Errors"/> that
/// prevented success and any non‑fatal <see cref="Warnings"/>. A result is a <b>success</b> exactly when it
/// has no errors. This is what an endpoint returns so the developer can surface errors and warnings without
/// throwing; the pipeline terminal maps it to a <see cref="PipelineResponse"/>.
/// </summary>
public sealed record Result
{
    private Result(IReadOnlyList<PipelineMessage> errors, IReadOnlyList<PipelineMessage> warnings)
    {
        Errors = errors;
        Warnings = warnings;
    }

    /// <summary>True when there are no errors.</summary>
    public bool IsSuccess => Errors.Count == 0;

    /// <summary>True when there is at least one error.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>Errors that prevented success. Empty on success.</summary>
    public IReadOnlyList<PipelineMessage> Errors { get; }

    /// <summary>Non‑fatal warnings. May be present on success or failure.</summary>
    public IReadOnlyList<PipelineMessage> Warnings { get; }

    /// <summary>A successful result.</summary>
    public static Result Success() => new([], []);

    /// <summary>A successful result with the given warnings.</summary>
    public static Result Success(params PipelineMessage[] warnings)
    {
        ArgumentNullException.ThrowIfNull(warnings);

        return new Result([], warnings);
    }

    /// <summary>A failed result with the given errors (at least one required).</summary>
    public static Result Failure(params PipelineMessage[] errors)
        => Failure((IEnumerable<PipelineMessage>)errors);

    /// <summary>A failed result with the given errors (at least one required).</summary>
    public static Result Failure(IEnumerable<PipelineMessage> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var list = errors as IReadOnlyList<PipelineMessage> ?? [.. errors];
        if (list.Count == 0)
        {
            throw new ArgumentException("A failure must contain at least one error.", nameof(errors));
        }

        return new Result(list, []);
    }

    /// <summary>Returns a copy with an extra warning appended.</summary>
    public Result WithWarning(PipelineMessage warning)
    {
        ArgumentNullException.ThrowIfNull(warning);
        return new Result(Errors, [.. Warnings, warning]);
    }

    /// <summary>Returns a copy with the given warnings appended.</summary>
    public Result WithWarnings(IEnumerable<PipelineMessage> warnings)
    {
        ArgumentNullException.ThrowIfNull(warnings);
        return new Result(Errors, [.. Warnings, .. warnings]);
    }
}
