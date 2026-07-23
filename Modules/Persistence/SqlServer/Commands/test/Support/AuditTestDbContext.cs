using Microsoft.EntityFrameworkCore;
using RaccoonLand.Core.Domain.Events;

namespace RaccoonLand.Modules.Persistence.SqlServer.Commands.Tests.Support;

/// <summary>
/// A plain (non-transactional) InMemory context used to exercise the audit interceptor against a real
/// <see cref="Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker"/>. It ignores the in-memory event
/// collections so the aggregate can be mapped by the InMemory provider.
/// </summary>
public sealed class AuditTestDbContext(DbContextOptions<AuditTestDbContext> options) : DbContext(options)
{
    public DbSet<AuditableRecord> AuditableRecords => Set<AuditableRecord>();

    public DbSet<TestAggregate> Aggregates => Set<TestAggregate>();

    public DbSet<PlainRecord> PlainRecords => Set<PlainRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<DomainEvent>();
        modelBuilder.Ignore<ServiceEvent>();
        modelBuilder.Entity<TestAggregate>().Ignore(nameof(TestAggregate.DomainEvents));
        modelBuilder.Entity<TestAggregate>().Ignore(nameof(TestAggregate.ServiceEvents));
    }
}
