using CleanArchitectureSample.Application.Departments.Queries.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaccoonLand.Modules.Persistence.EntityFrameworkCore.Configuration;

namespace CleanArchitectureSample.Infrastructure.Persistence.Queries.Departments;

public sealed class DepartmentReadModelConfiguration
    : IEntityTypeConfiguration<DepartmentReadModel>, IQueryEntityConfiguration
{
    public void Configure(EntityTypeBuilder<DepartmentReadModel> builder)
    {
        builder.ToTable("Departments");
    }
}
