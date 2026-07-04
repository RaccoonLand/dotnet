namespace RaccoonLand.Core.Domain.Exceptions;

/// <summary>
/// Domain exception. Whenever this is thrown, the pipeline exception-handling middleware returns an HTTP 400
/// response and exposes each error's <see cref="DomainError.Code"/> as a machine-readable contract to the consumer.
/// <see cref="Exception.Message"/> concatenates every error's localization template key (see
/// <see cref="DomainError.Message"/>).
/// </summary>
public class DomainException : Exception
{
    /// <param name="code">Error code (required) used to inform the consumer.</param>
    /// <param name="message">
    /// Localization template key (required). When no <c>IMessageLocalization</c> is available, this value is
    /// shown as the user-facing message.
    /// </param>
    /// <param name="parameters">
    /// Optional positional parameters inserted into the localized template (same convention as
    /// <c>IMessageLocalization</c>).
    /// </param>
    public DomainException(string code, string message, params object?[] parameters)
        : this(new DomainError(code, message, parameters))
    {
    }

    /// <param name="errors">One or more domain errors. At least one is required.</param>
    public DomainException(params DomainError[] errors)
        : base(CombineMessages(errors))
    {
        ArgumentNullException.ThrowIfNull(errors);

        if (errors.Length == 0)
        {
            throw new ArgumentException("At least one error is required.", nameof(errors));
        }

        Errors = errors;
    }

    /// <summary>All domain errors carried by this exception (never empty).</summary>
    public IReadOnlyList<DomainError> Errors { get; }

    private static string CombineMessages(DomainError[] errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        if (errors.Length == 0)
        {
            throw new ArgumentException("At least one error is required.", nameof(errors));
        }

        return string.Join("; ", errors.Select(error => error.Message));
    }
}
