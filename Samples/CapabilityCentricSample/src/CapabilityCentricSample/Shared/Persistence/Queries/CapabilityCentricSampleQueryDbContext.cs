using CapabilityCentricSample.Departments.Persistence.Queries;
using CapabilityCentricSample.People.Persistence.Queries;
using Microsoft.EntityFrameworkCore;
using RaccoonLand.Modules.Persistence.EntityFrameworkCore.Configuration;
using RaccoonLand.Modules.Persistence.EntityFrameworkCore.Extensions;
using RaccoonLand.Modules.Persistence.SqlServer.Queries;

namespace CapabilityCentricSample.Shared.Persistence.Queries;

public sealed class CapabilityCentricSampleQueryDbContext : BaseQueryDbContext
{
    public CapabilityCentricSampleQueryDbContext(DbContextOptions<CapabilityCentricSampleQueryDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly<IQueryEntityConfiguration>(
            typeof(CapabilityCentricSampleQueryDbContext).Assembly);
    }

    public DbSet<Person> Persons { get; set; }

    public DbSet<Department> Departments { get; set; }
}
