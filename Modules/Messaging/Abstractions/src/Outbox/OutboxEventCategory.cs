namespace RaccoonLand.Modules.Messaging.Abstractions;

/// <summary>
/// Well-known values for <see cref="OutboxEventRecord.Category"/> written by the command-side
/// outbox interceptor (<c>"Domain"</c> / <c>"Service"</c>).
/// </summary>
public static class OutboxEventCategory
{
    public const string Domain = "Domain";

    public const string Service = "Service";

    /// <summary>Returns true when <paramref name="category"/> is <see cref="Domain"/> or <see cref="Service"/>.</summary>
    public static bool IsKnown(string? category)
        => category is Domain or Service;

    /// <summary>
    /// Throws <see cref="ArgumentException"/> when <paramref name="category"/> is not a known value.
    /// </summary>
    public static string EnsureKnown(string? category, string paramName = "category")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category, paramName);

        if (!IsKnown(category))
        {
            throw new ArgumentException(
                $"Unknown outbox Category '{category}'. Expected '{Domain}' or '{Service}'.",
                paramName);
        }

        return category;
    }
}
