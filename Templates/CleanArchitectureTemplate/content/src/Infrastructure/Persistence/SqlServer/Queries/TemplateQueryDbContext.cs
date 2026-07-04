using CleanArchitectureTemplate.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using RaccoonLand.Modules.Persistence.EntityFrameworkCore.Configuration;
using RaccoonLand.Modules.Persistence.EntityFrameworkCore.Extensions;
using RaccoonLand.Modules.Persistence.SqlServer.Queries;

namespace CleanArchitectureTemplate.Infrastructure.Persistence.Queries;

public sealed class TemplateQueryDbContext
    : BaseQueryDbContext, IQueryDbContext
{
    public TemplateQueryDbContext(DbContextOptions<TemplateQueryDbContext> options)
        : base(options)
    {
    }

    public new DbSet<TEntity> Set<TEntity>() where TEntity : class
        => base.Set<TEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly<IQueryEntityConfiguration>(
            typeof(TemplateQueryDbContext).Assembly);
    }
}