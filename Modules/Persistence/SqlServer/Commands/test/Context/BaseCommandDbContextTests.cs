using Microsoft.EntityFrameworkCore;
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
