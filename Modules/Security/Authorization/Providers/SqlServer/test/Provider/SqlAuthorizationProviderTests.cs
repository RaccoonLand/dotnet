using RaccoonLand.Modules.Security.Authorization.Abstractions;
using RaccoonLand.Modules.Security.Authorization.SqlServer.Configuration;
using RaccoonLand.Modules.Security.Authorization.SqlServer.Tests.Support;

namespace RaccoonLand.Modules.Security.Authorization.SqlServer.Tests.Provider;

public sealed class SqlAuthorizationProviderTests
{
    private const string RequestName = "Sample.Namespace.SampleRequest";

    [Fact]
    public async Task AuthorizeAsync_WhenAnonymous_AllowsEvenWithoutUser()
    {
        var repository = new FakeSqlAuthorizationRepository { AnonymousResult = [RequestName] };
        var provider = SqlAuthorizationTestHelpers.CreateProvider(repository, userId: null);

        var decision = await Authorize(provider);

        Assert.Equal(AuthorizationStatus.Allowed, decision.Status);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenNoUserId_ReturnsUnauthenticated()
    {
        var repository = new FakeSqlAuthorizationRepository();
        var provider = SqlAuthorizationTestHelpers.CreateProvider(repository, userId: null);

        var decision = await Authorize(provider);

        Assert.Equal(AuthorizationStatus.Unauthenticated, decision.Status);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenRequestInUserAllowedSet_ReturnsAllowed()
    {
        var repository = new FakeSqlAuthorizationRepository { AllowedResult = [RequestName] };
        var provider = SqlAuthorizationTestHelpers.CreateProvider(repository, userId: "user-1");

        var decision = await Authorize(provider);

        Assert.Equal(AuthorizationStatus.Allowed, decision.Status);
        Assert.Equal("user-1", repository.LastUserId);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenRequestNotInUserAllowedSet_ReturnsDenied()
    {
        var repository = new FakeSqlAuthorizationRepository { AllowedResult = ["Other.Request"] };
        var provider = SqlAuthorizationTestHelpers.CreateProvider(repository, userId: "user-1");

        var decision = await Authorize(provider);

        Assert.Equal(AuthorizationStatus.Denied, decision.Status);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenAnonymousLookupFails_PropagatesExceptionNotDenied()
    {
        var repository = new FakeSqlAuthorizationRepository
        {
            AnonymousException = new InvalidOperationException("db down"),
        };
        var provider = SqlAuthorizationTestHelpers.CreateProvider(repository, userId: "user-1");

        await Assert.ThrowsAsync<InvalidOperationException>(() => Authorize(provider));
    }

    [Fact]
    public async Task AuthorizeAsync_WhenAllowedLookupFails_PropagatesExceptionNotDenied()
    {
        var repository = new FakeSqlAuthorizationRepository
        {
            AllowedException = new InvalidOperationException("db down"),
        };
        var provider = SqlAuthorizationTestHelpers.CreateProvider(repository, userId: "user-1");

        await Assert.ThrowsAsync<InvalidOperationException>(() => Authorize(provider));
    }

    [Fact]
    public void Constructor_WhenCacheEnabledWithoutDistributedCache_Throws()
    {
        var options = new SqlAuthorizationOptions { EnableCache = true };

        Assert.Throws<InvalidOperationException>(() =>
            SqlAuthorizationTestHelpers.CreateProvider(
                new FakeSqlAuthorizationRepository(),
                userId: "user-1",
                options: options));
    }

    private static Task<AuthorizationDecision> Authorize(SqlServer.Provider.SqlAuthorizationProvider provider)
        => provider.AuthorizeAsync(new AuthorizationContext(RequestName), CancellationToken.None);
}
