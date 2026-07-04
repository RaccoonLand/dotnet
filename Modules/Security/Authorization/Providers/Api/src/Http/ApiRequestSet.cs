namespace RaccoonLand.Modules.Security.Authorization.Api.Http;

/// <summary>
/// Response body of both authorization-API endpoints: the set of request full-names (the values of
/// <c>request.GetType().FullName</c>). Modeled as an object (rather than a bare array) so the contract can grow
/// without breaking clients.
/// </summary>
public sealed class ApiRequestSet
{
    /// <summary>The request full-names returned by the endpoint.</summary>
    public IReadOnlyList<string> Requests { get; init; } = [];
}
