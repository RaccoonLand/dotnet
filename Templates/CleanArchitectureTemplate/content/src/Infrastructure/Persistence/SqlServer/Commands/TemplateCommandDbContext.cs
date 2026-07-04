using CleanArchitectureTemplate.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using RaccoonLand.Modules.Persistence.EntityFrameworkCore.Configuration;
using RaccoonLand.Modules.Persistence.EntityFrameworkCore.Extensions;
using RaccoonLand.Modules.Persistence.SqlServer.Commands;

namespace CleanArchitectureTemplate.Infrastructure.Persistence.Commands;

public sealed class TemplateCommandDbContext
    : BaseCommandDbContext, ICommandDbContext
{
    public TemplateCommandDbContext(DbContextOptions<TemplateCommandDbContext> options)
        : base(options)
    {
    }

    public new DbSet<TEntity> Set<TEntity>() where TEntity : class
        => base.Set<TEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly<ICommandEntityConfiguration>(
            typeof(TemplateCommandDbContext).Assembly);
    }
}