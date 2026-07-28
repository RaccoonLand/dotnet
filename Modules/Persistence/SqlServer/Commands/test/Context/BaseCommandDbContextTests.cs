using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RaccoonLand.Core.Domain.Abstractions;
using RaccoonLand.Core.Domain.Events;
using RaccoonLand.Modules.Persistence.SqlServer.Commands.Tests.Support;

namespace RaccoonLand.Modules.Persistence.SqlServer.Commands.Tests.Context;

public sealed class BaseCommandDbContextTests
{
    [Fact]
    public void SaveChanges_IsNotSupported()
    {
        using var context = PersistenceTestHelpers.CreateCommandContext();

        Assert.Throws<NotSupportedException>(() => context.SaveChanges());
    }

    [Fact]
    public void ResetForRetry_WhenOutboxWriterNotRegistered_DoesNotThrow()
    {
        // Sample/Template hosts register only the domain/service event outbox. Retry cleanup must
        // treat OutboxWriter as optional (EF typed GetService throws when unregistered).
        using var context = PersistenceTestHelpers.CreateCommandContext();

        OutboxAttemptCleanup.ResetForRetry(context);
    }

    [Fact]
    public async Task SaveChangesAsync_WithoutMessageOutboxWriter_Succeeds()
    {
        // Owned-transaction path calls ResetForRetry before BeginTransaction; InMemory needs the
        // transaction-ignored warning suppressed so the save can complete.
        var options = new DbContextOptionsBuilder<TestCommandDbContext>()
            .UseInMemoryDatabase($"command-no-writer-{Guid.NewGuid():N}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        await using var context = new TestCommandDbContext(options);
        context.Aggregates.Add(new TestAggregate { Name = "without-message-outbox" });

        var affected = await context.SaveChangesAsync();

        Assert.Equal(1, affected);
    }

    [Fact]
    public void SaveChanges_WithAcceptAllChangesFlag_IsNotSupported()
    {
        using var context = PersistenceTestHelpers.CreateCommandContext();

        Assert.Throws<NotSupportedException>(() => context.SaveChanges(acceptAllChangesOnSuccess: true));
    }

    [Fact]
    public void OnModelCreating_ExcludesDomainAndServiceEventTypes()
    {
        using var context = PersistenceTestHelpers.CreateCommandContext();

        var model = context.Model;

        Assert.Null(model.FindEntityType(typeof(DomainEvent)));
        Assert.Null(model.FindEntityType(typeof(ServiceEvent)));
    }

    [Fact]
    public void OnModelCreating_ExcludesAggregateEventCollections()
    {
        using var context = PersistenceTestHelpers.CreateCommandContext();

        var aggregate = context.Model.FindEntityType(typeof(TestAggregate));

        Assert.NotNull(aggregate);
        Assert.Null(aggregate!.FindNavigation(nameof(IAggregateRoot.DomainEvents)));
        Assert.Null(aggregate.FindProperty(nameof(IAggregateRoot.DomainEvents)));
        Assert.Null(aggregate.FindNavigation(nameof(IAggregateRoot.ServiceEvents)));
        Assert.Null(aggregate.FindProperty(nameof(IAggregateRoot.ServiceEvents)));
    }
}
