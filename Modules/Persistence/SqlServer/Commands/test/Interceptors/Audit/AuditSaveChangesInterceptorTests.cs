using RaccoonLand.Core.Domain.Abstractions;
using RaccoonLand.Modules.Persistence.SqlServer.Commands.Tests.Support;

namespace RaccoonLand.Modules.Persistence.SqlServer.Commands.Tests.Interceptors.Audit;

public sealed class AuditSaveChangesInterceptorTests
{
    [Fact]
    public async Task SavingChanges_WhenAdded_StampsCreatedAuditWithUser()
    {
        var context = PersistenceTestHelpers.CreateAuditContext(
            new FakeExecutionContext { IsAvailable = true, UserId = "user-42" });
        var record = new AuditableRecord { Name = "a" };
        context.AuditableRecords.Add(record);

        await context.SaveChangesAsync();

        Assert.NotEqual(default, record.CreatedAtUtc);
        Assert.Equal("user-42", record.CreatedBy);
        Assert.Null(record.ModifiedAtUtc);
        Assert.Null(record.ModifiedBy);
    }

    [Fact]
    public async Task SavingChanges_WhenModified_StampsModifiedAudit()
    {
        var context = PersistenceTestHelpers.CreateAuditContext(
            new FakeExecutionContext { IsAvailable = true, UserId = "creator" });
        var record = new AuditableRecord { Name = "a" };
        context.AuditableRecords.Add(record);
        await context.SaveChangesAsync();

        record.Name = "b";
        await context.SaveChangesAsync();

        Assert.NotNull(record.ModifiedAtUtc);
        Assert.Equal("creator", record.ModifiedBy);
        // The created stamp is preserved across the modify.
        Assert.NotEqual(default, record.CreatedAtUtc);
    }

    [Fact]
    public async Task SavingChanges_WhenAggregateModified_RegeneratesConcurrencyToken()
    {
        var context = PersistenceTestHelpers.CreateAuditContext(null);
        var aggregate = new TestAggregate { Name = "a" };
        context.Aggregates.Add(aggregate);
        await context.SaveChangesAsync();
        var tokenAfterCreate = aggregate.ConcurrencyToken;

        aggregate.Name = "b";
        await context.SaveChangesAsync();

        Assert.NotEqual(tokenAfterCreate, aggregate.ConcurrencyToken);
    }

    [Fact]
    public async Task SavingChanges_WhenUnchanged_DoesNotStampModifiedOrRegenerateToken()
    {
        var context = PersistenceTestHelpers.CreateAuditContext(null);
        var aggregate = new TestAggregate { Name = "a" };
        context.Aggregates.Add(aggregate);
        await context.SaveChangesAsync();
        var tokenAfterCreate = aggregate.ConcurrencyToken;

        // No changes -> the entry is Unchanged on the second save.
        await context.SaveChangesAsync();

        Assert.Null(aggregate.ModifiedAtUtc);
        Assert.Equal(tokenAfterCreate, aggregate.ConcurrencyToken);
    }

    [Fact]
    public async Task SavingChanges_WhenDeleted_DoesNotStampModifiedOrRegenerateToken()
    {
        var context = PersistenceTestHelpers.CreateAuditContext(null);
        var aggregate = new TestAggregate { Name = "a" };
        context.Aggregates.Add(aggregate);
        await context.SaveChangesAsync();
        var tokenAfterCreate = aggregate.ConcurrencyToken;

        context.Aggregates.Remove(aggregate);
        await context.SaveChangesAsync();

        Assert.Null(aggregate.ModifiedAtUtc);
        Assert.Equal(tokenAfterCreate, aggregate.ConcurrencyToken);
    }

    [Fact]
    public async Task SavingChanges_WhenNonAuditableEntity_IsIgnoredWithoutError()
    {
        var context = PersistenceTestHelpers.CreateAuditContext(
            new FakeExecutionContext { IsAvailable = true, UserId = "user-1" });
        var plain = new PlainRecord { Name = "a" };
        context.PlainRecords.Add(plain);

        var affected = await context.SaveChangesAsync();

        Assert.Equal(1, affected);
    }

    [Fact]
    public async Task SavingChanges_WhenNoExecutionContext_StampsTimestampWithNullUser()
    {
        var context = PersistenceTestHelpers.CreateAuditContext(null);
        var record = new AuditableRecord { Name = "a" };
        context.AuditableRecords.Add(record);

        await context.SaveChangesAsync();

        Assert.NotEqual(default, record.CreatedAtUtc);
        Assert.Null(record.CreatedBy);
    }

    [Fact]
    public void SaveChanges_Sync_StampsAuditLikeAsync()
    {
        var context = PersistenceTestHelpers.CreateAuditContext(
            new FakeExecutionContext { IsAvailable = true, UserId = "sync-user" });
        var record = new AuditableRecord { Name = "a" };
        context.AuditableRecords.Add(record);

        context.SaveChanges();

        Assert.NotEqual(default, record.CreatedAtUtc);
        Assert.Equal("sync-user", record.CreatedBy);
    }
}
