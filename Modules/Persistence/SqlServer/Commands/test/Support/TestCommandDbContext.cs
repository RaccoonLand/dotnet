using Microsoft.EntityFrameworkCore;

namespace RaccoonLand.Modules.Persistence.SqlServer.Commands.Tests.Support;

/// <summary>
/// Concrete <see cref="BaseCommandDbContext"/> used to verify the DB-agnostic parts of the base context
/// (synchronous-save guard and model event exclusion). Transactional orchestration is covered by the
/// SQL Server integration suite, not here.
/// </summary>
public sealed class TestCommandDbContext(DbContextOptions options) : BaseCommandDbContext(options)
{
    public DbSet<TestAggregate> Aggregates => Set<TestAggregate>();
}
