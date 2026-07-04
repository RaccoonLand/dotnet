using CapabilityCentricTemplate.People.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using RaccoonLand.Modules.Persistence.EntityFrameworkCore.Configuration;
using RaccoonLand.Modules.Persistence.EntityFrameworkCore.Extensions;
using RaccoonLand.Modules.Persistence.SqlServer.Commands;

namespace CapabilityCentricTemplate.Shared.Persistence.Commands;

public sealed class TemplateCommandDbContext : BaseCommandDbContext
{
    public TemplateCommandDbContext(DbContextOptions<TemplateCommandDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly<ICommandEntityConfiguration>(
            typeof(TemplateCommandDbContext).Assembly);
    }

    public DbSet<Person> Persons { get; set; }
}
