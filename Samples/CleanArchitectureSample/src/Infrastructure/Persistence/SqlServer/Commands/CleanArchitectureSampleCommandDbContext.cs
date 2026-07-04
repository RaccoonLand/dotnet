using CleanArchitectureSample.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using RaccoonLand.Modules.Persistence.EntityFrameworkCore.Configuration;
using RaccoonLand.Modules.Persistence.EntityFrameworkCore.Extensions;
using RaccoonLand.Modules.Persistence.SqlServer.Commands;

namespace CleanArchitectureSample.Infrastructure.Persistence.Commands;

public sealed class CleanArchitectureSampleCommandDbContext
    : BaseCommandDbContext, ICommandDbContext
{
    public CleanArchitectureSampleCommandDbContext(
        DbContextOptions<CleanArchitectureSampleCommandDbContext> options)
        : base(options)
    {
    }

    public new DbSet<TEntity> Set<TEntity>() where TEntity : class
        => base.Set<TEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly<ICommandEntityConfiguration>(
            typeof(CleanArchitectureSampleCommandDbContext).Assembly);
    }
}
