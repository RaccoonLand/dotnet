using CleanArchitectureSample.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using RaccoonLand.Modules.Persistence.EntityFrameworkCore.Configuration;
using RaccoonLand.Modules.Persistence.EntityFrameworkCore.Extensions;
using RaccoonLand.Modules.Persistence.SqlServer.Queries;

namespace CleanArchitectureSample.Infrastructure.Persistence.Queries;

public sealed class CleanArchitectureSampleQueryDbContext
    : BaseQueryDbContext, IQueryDbContext
{
    public CleanArchitectureSampleQueryDbContext(
        DbContextOptions<CleanArchitectureSampleQueryDbContext> options)
        : base(options)
    {
    }

    public new DbSet<TEntity> Set<TEntity>() where TEntity : class
        => base.Set<TEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly<IQueryEntityConfiguration>(
            typeof(CleanArchitectureSampleQueryDbContext).Assembly);
    }
}
