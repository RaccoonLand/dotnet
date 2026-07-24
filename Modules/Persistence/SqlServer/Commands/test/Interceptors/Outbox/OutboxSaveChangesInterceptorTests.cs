using Microsoft.EntityFrameworkCore;
using RaccoonLand.Modules.Persistence.SqlServer.Commands.Interceptors;
using RaccoonLand.Modules.Persistence.SqlServer.Commands.Outbox;
using RaccoonLand.Modules.Persistence.SqlServer.Commands.Tests.Support;

namespace RaccoonLand.Modules.Persistence.SqlServer.Commands.Tests.Interceptors.Outbox;

public sealed class OutboxSaveChangesInterceptorTests
{
    [Fact]
    public async Task SavedChangesAsync_WhenAggregateHasEventsAndNoAmbientTransaction_Throws()
    {
        // Attaching the outbox interceptor to a plain DbContext (InMemory) with no ambient transaction is
        // exactly the misuse scenario the guard is meant to catch loudly. The buggy old branch would silently
        // auto-commit the outbox INSERT and break atomicity.
        var interceptor = new OutboxSaveChangesInterceptor(new OutboxOptions { Table = "OutboxEvent" });
        var options = new DbContextOptionsBuilder<AuditTestDbContext>()
            .UseInMemoryDatabase($"outbox-no-tx-{Guid.NewGuid():N}")
            .AddInterceptors(interceptor)
            .Options;

        await using var context = new AuditTestDbContext(options);
        var aggregate = new TestAggregate { Name = "a" };
        aggregate.RaiseDomain(new TestDomainEvent { Data = "x" });
        context.Aggregates.Add(aggregate);

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task SavedChangesAsync_WhenNoAggregatesHaveEvents_DoesNotThrowEvenWithoutTransaction()
    {
        // The interceptor short-circuits before the ambient-transaction check when there is nothing to write,
        // so a save with no pending events must still work on any context.
        var interceptor = new OutboxSaveChangesInterceptor(new OutboxOptions { Table = "OutboxEvent" });
        var options = new DbContextOptionsBuilder<AuditTestDbContext>()
            .UseInMemoryDatabase($"outbox-empty-{Guid.NewGuid():N}")
            .AddInterceptors(interceptor)
            .Options;

        await using var context = new AuditTestDbContext(options);
        var aggregate = new TestAggregate { Name = "a" };
        context.Aggregates.Add(aggregate);

        var affected = await context.SaveChangesAsync();

        Assert.Equal(1, affected);
    }

    [Fact]
    public void ResetAttemptState_WhenNoStateHasBeenCreated_DoesNotThrow()
    {
        // Reset is called at the start of every execution-strategy attempt; it must be a no-op the first time
        // when the ConditionalWeakTable has never seen this DbContext.
        var options = new DbContextOptionsBuilder<AuditTestDbContext>()
            .UseInMemoryDatabase($"outbox-reset-{Guid.NewGuid():N}")
            .Options;

        using var context = new AuditTestDbContext(options);

        OutboxSaveChangesInterceptor.ResetAttemptState(context);
    }

    [Fact]
    public void ResetAttemptState_WhenContextNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => OutboxSaveChangesInterceptor.ResetAttemptState(null!));
    }
}
