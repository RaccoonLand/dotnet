using CapabilityCentricTemplate.People.Persistence.Queries;
using Microsoft.EntityFrameworkCore;
using RaccoonLand.Modules.Persistence.EntityFrameworkCore.Configuration;
using RaccoonLand.Modules.Persistence.EntityFrameworkCore.Extensions;
using RaccoonLand.Modules.Persistence.SqlServer.Queries;

namespace CapabilityCentricTemplate.Shared.Persistence.Queries;

public sealed class TemplateQueryDbContext : BaseQueryDbContext
{
    public TemplateQueryDbContext(DbContextOptions<TemplateQueryDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly<IQueryEntityConfiguration>(
            typeof(TemplateQueryDbContext).Assembly);
    }

    public DbSet<Person> Persons { get; set; }
}
