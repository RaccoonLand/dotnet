using CapabilityCentricSample.Departments.Domain.Entities;
using CapabilityCentricSample.People.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using RaccoonLand.Modules.Persistence.EntityFrameworkCore.Configuration;
using RaccoonLand.Modules.Persistence.EntityFrameworkCore.Extensions;
using RaccoonLand.Modules.Persistence.SqlServer.Commands;

namespace CapabilityCentricSample.Shared.Persistence.Commands;

public sealed class CapabilityCentricSampleCommandDbContext : BaseCommandDbContext
{
    public CapabilityCentricSampleCommandDbContext(DbContextOptions<CapabilityCentricSampleCommandDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly<ICommandEntityConfiguration>(
            typeof(CapabilityCentricSampleCommandDbContext).Assembly);
    }

    public DbSet<Person> Persons { get; set; }

    public DbSet<Department> Departments { get; set; }
}
