namespace RaccoonLand.Core.Domain.Exceptions;

/// <summary>
/// A single domain error: a machine-readable <see cref="Code"/>, a localization template key in
/// <see cref="Message"/>, and optional positional <see cref="Parameters"/>.
/// </summary>
public sealed class DomainError
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
    public DomainError(string code, string message, params object?[] parameters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Code = code;
        Message = message;
        // Defensive copy so callers cannot mutate this instance after construction.
        Parameters = parameters is { Length: > 0 } ? parameters.ToArray() : [];
    }

    /// <summary>Error code for the consumer's programmed reaction.</summary>
    public string Code { get; }

    /// <summary>Localization template key (never null/whitespace).</summary>
    public string Message { get; }

    /// <summary>Positional parameters for message localization (never null; read-only).</summary>
    public IReadOnlyList<object?> Parameters { get; }
}
