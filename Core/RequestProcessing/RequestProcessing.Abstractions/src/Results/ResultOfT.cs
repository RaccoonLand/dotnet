using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;

namespace RaccoonLand.Core.RequestProcessing.Abstractions.Results;

/// <summary>
/// The outcome of an endpoint that produces a <typeparamref name="TValue"/> payload. On success
/// <see cref="Value"/> holds the payload and <see cref="Errors"/> is empty; on failure <see cref="Value"/> is
/// default and <see cref="Errors"/> describes what went wrong. <see cref="Warnings"/> may accompany either.
/// <para>
/// A <typeparamref name="TValue"/> converts implicitly to a successful <see cref="Result{TValue}"/>, so an
/// endpoint can simply <c>return value;</c> on the happy path.
/// </para>
/// </summary>
public sealed record Result<TValue>
{
    private Result(TValue? value, IReadOnlyList<PipelineMessage> errors, IReadOnlyList<PipelineMessage> warnings)
    {
        Value = value;
        Errors = errors;
        Warnings = warnings;
    }

    /// <summary>True when there are no errors.</summary>
    public bool IsSuccess => Errors.Count == 0;

    /// <summary>True when there is at least one error.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>The payload on success; default when the result is a failure.</summary>
    public TValue? Value { get; }

    /// <summary>Errors that prevented success. Empty on success.</summary>
    public IReadOnlyList<PipelineMessage> Errors { get; }

    /// <summary>Non‑fatal warnings. May be present on success or failure.</summary>
    public IReadOnlyList<PipelineMessage> Warnings { get; }

    /// <summary>A successful result carrying <paramref name="value"/>.</summary>
    public static Result<TValue> Success(TValue value) => new(value, [], []);

    /// <summary>A successful result carrying <paramref name="value"/> and the given warnings.</summary>
    public static Result<TValue> Success(TValue value, params PipelineMessage[] warnings)
    {
        ArgumentNullException.ThrowIfNull(warnings);

        return new Result<TValue>(value, [], warnings);
    }

    /// <summary>A failed result with the given errors (at least one required).</summary>
    public static Result<TValue> Failure(params PipelineMessage[] errors)
        => Failure((IEnumerable<PipelineMessage>)errors);

    /// <summary>A failed result with the given errors (at least one required).</summary>
    public static Result<TValue> Failure(IEnumerable<PipelineMessage> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var list = errors as IReadOnlyList<PipelineMessage> ?? [.. errors];
        if (list.Count == 0)
        {
            throw new ArgumentException("A failure must contain at least one error.", nameof(errors));
        }

        return new Result<TValue>(default, list, []);
    }

    /// <summary>Returns a copy with an extra warning appended.</summary>
    public Result<TValue> WithWarning(PipelineMessage warning)
    {
        ArgumentNullException.ThrowIfNull(warning);
        return new Result<TValue>(Value, Errors, [.. Warnings, warning]);
    }

    /// <summary>Returns a copy with the given warnings appended.</summary>
    public Result<TValue> WithWarnings(IEnumerable<PipelineMessage> warnings)
    {
        ArgumentNullException.ThrowIfNull(warnings);
        return new Result<TValue>(Value, Errors, [.. Warnings, .. warnings]);
    }

    /// <summary>Implicitly wraps a value as a successful result.</summary>
    public static implicit operator Result<TValue>(TValue value) => Success(value);
}
