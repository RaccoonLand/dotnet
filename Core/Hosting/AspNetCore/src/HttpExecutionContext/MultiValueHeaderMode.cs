namespace RaccoonLand.Core.Hosting.AspNetCore.HttpExecutionContext;

/// <summary>
/// How to interpret a request header that may carry multiple values
/// (<see cref="Microsoft.Extensions.Primitives.StringValues"/>).
/// </summary>
public enum MultiValueHeaderMode
{
    /// <summary>Use the first non-empty value; ignore the rest.</summary>
    FirstValue = 0,

    /// <summary>Accept the header only when it has exactly one non-empty value; otherwise treat as missing.</summary>
    SingleValueOnly = 1,

    /// <summary>
    /// Use <see cref="Microsoft.Extensions.Primitives.StringValues.ToString"/> (comma-joined when multiple).
    /// Permissive / legacy behavior.
    /// </summary>
    Join = 2,
}
