using Microsoft.Extensions.Options;
using RaccoonLand.Core.ExecutionContext.Abstractions;
using RaccoonLand.Modules.Security.Authorization.SqlServer.Configuration;
using RaccoonLand.Modules.Security.Authorization.SqlServer.Data;
using RaccoonLand.Modules.Security.Authorization.SqlServer.Provider;

namespace RaccoonLand.Modules.Security.Authorization.SqlServer.Tests.Support;

internal sealed class FakeExecutionContext : ICurrentExecutionContext
{
    public bool IsAvailable => UserId is not null;
    public string? UserId { get; init; }
    public string? TenantId { get; init; }
    public string? CorrelationId { get; init; }
}

internal sealed class FakeSqlAuthorizationRepository : ISqlAuthorizationRepository
{
    public IReadOnlyCollection<string> AnonymousResult { get; set; } = [];
    public IReadOnlyCollection<string> AllowedResult { get; set; } = [];
    public Exception? AnonymousException { get; set; }
    public Exception? AllowedException { get; set; }
    public string? LastUserId { get; private set; }

    public Task<IReadOnlyCollection<string>> GetAnonymousRequestsAsync(CancellationToken cancellationToken)
        => AnonymousException is not null
            ? Task.FromException<IReadOnlyCollection<string>>(AnonymousException)
            : Task.FromResult(AnonymousResult);

    public Task<IReadOnlyCollection<string>> GetAllowedRequestsAsync(string userId, CancellationToken cancellationToken)
    {
        LastUserId = userId;
        return AllowedException is not null
            ? Task.FromException<IReadOnlyCollection<string>>(AllowedException)
            : Task.FromResult(AllowedResult);
    }
}

internal static class SqlAuthorizationTestHelpers
{
    public static SqlAuthorizationProvider CreateProvider(
        ISqlAuthorizationRepository repository,
        string? userId,
        SqlAuthorizationOptions? options = null)
        => new(
            repository,
            new FakeExecutionContext { UserId = userId },
            Options.Create(options ?? new SqlAuthorizationOptions()));
}
