using Microsoft.EntityFrameworkCore;

namespace RaccoonLand.Modules.Persistence.SqlServer.Queries.Tests.Context;

public sealed class BaseQueryDbContextTests
{
    private sealed class TestQueryContext(DbContextOptions options) : BaseQueryDbContext(options)
    {
        public DbSet<QueryItem> Items => Set<QueryItem>();
    }

    private sealed class QueryItem
    {
        public int Id { get; set; }
    }

    private static TestQueryContext Create()
    {
        var options = new DbContextOptionsBuilder<TestQueryContext>()
            .UseInMemoryDatabase($"query-{Guid.NewGuid():N}")
            .Options;
        return new TestQueryContext(options);
    }

    [Fact]
    public void SaveChanges_IsBlocked()
    {
        using var context = Create();

        Assert.Throws<InvalidOperationException>(() => context.SaveChanges());
    }

    [Fact]
    public void SaveChanges_WithFlag_IsBlocked()
    {
        using var context = Create();

        Assert.Throws<InvalidOperationException>(() => context.SaveChanges(true));
    }

    [Fact]
    public async Task SaveChangesAsync_IsBlocked()
    {
        await using var context = Create();

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChangesAsync_WithFlag_IsBlocked()
    {
        await using var context = Create();

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync(true));
    }

    [Fact]
    public void ChangeTrackerDefaults_AreReadOnly()
    {
        using var context = Create();

        Assert.Equal(QueryTrackingBehavior.NoTracking, context.ChangeTracker.QueryTrackingBehavior);
        Assert.False(context.ChangeTracker.AutoDetectChangesEnabled);
    }
}
