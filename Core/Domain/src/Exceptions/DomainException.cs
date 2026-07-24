namespace RaccoonLand.Core.Domain.Exceptions;

/// <summary>
/// Domain exception that carries one or more <see cref="DomainError"/> values for business-rule violations.
/// <see cref="Exception.Message"/> concatenates every error's localization template key (see
/// <see cref="DomainError.Message"/>). How a host maps this exception to a transport response is defined
/// by that host (for example request-pipeline exception handling), not by this type.
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

    /// <param name="errors">
    /// One or more domain errors. At least one is required. The array itself must not be <see langword="null"/>,
    /// and no element within it may be <see langword="null"/> — both cases throw <see cref="ArgumentException"/>
    /// (or <see cref="ArgumentNullException"/> for a null array).
    /// </param>
    public DomainException(params DomainError[] errors)
        : base(BuildMessage(Validate(errors)))
    {
        Errors = errors;
    }

    /// <summary>All domain errors carried by this exception (never empty).</summary>
    public IReadOnlyList<DomainError> Errors { get; }

    private static DomainError[] Validate(DomainError[] errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        if (errors.Length == 0)
        {
            throw new ArgumentException("At least one error is required.", nameof(errors));
        }

        // Reject null elements up front so consumers never observe a NullReferenceException from
        // downstream message composition (that would look like an internal bug, not bad input).
        for (var i = 0; i < errors.Length; i++)
        {
            if (errors[i] is null)
            {
                throw new ArgumentException(
                    $"errors[{i}] is null. Every element of errors must be a non-null DomainError.",
                    nameof(errors));
            }
        }

        return errors;
    }

    private static string BuildMessage(DomainError[] errors)
        => string.Join("; ", errors.Select(error => error.Message));
}
