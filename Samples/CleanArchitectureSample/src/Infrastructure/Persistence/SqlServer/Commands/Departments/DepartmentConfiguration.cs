using CleanArchitectureSample.Departments.Domain.Entities;
using CleanArchitectureSample.Departments.Shared;
using CleanArchitectureSample.Shared.Localizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaccoonLand.Modules.Persistence.EntityFrameworkCore.Configuration;

namespace CleanArchitectureSample.Infrastructure.Persistence.Commands.Departments;

public sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>, ICommandEntityConfiguration
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");

        builder.OwnsOne(x => x.Code, ownedBuilder =>
        {
            ownedBuilder.Property(x => x.Value).HasColumnName(nameof(Department.Code)).HasMaxLength(DepartmentConstants.DEPARTMENT_CODE_MAX_LENGTH);
            ownedBuilder.HasIndex(x => x.Value).IsUnique();
        });

        builder.OwnsOne(x => x.Name, ownedBuilder =>
        {
            ownedBuilder.Property(x => x.Value).HasColumnName(nameof(Department.Name)).HasMaxLength(DepartmentConstants.DEPARTMENT_NAME_MAX_LENGTH);
        });

        builder.OwnsOne(x => x.Description, ownedBuilder =>
        {
            ownedBuilder.Property(x => x.Value).HasColumnName(nameof(Department.Description)).HasMaxLength(SharedConstants.DESCRIPTION_MAX_LENGTH);
        });
    }
}
