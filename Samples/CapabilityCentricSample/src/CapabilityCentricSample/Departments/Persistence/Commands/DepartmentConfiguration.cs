using CapabilityCentricSample.Departments.Domain.Entities;
using CapabilityCentricSample.Departments.Shared;
using CapabilityCentricSample.Shared.Localizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaccoonLand.Modules.Persistence.EntityFrameworkCore.Configuration;

namespace CapabilityCentricSample.Departments.Persistence.Commands;

public sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>, ICommandEntityConfiguration
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Status)
            .HasColumnType("tinyint");

        builder.OwnsOne(x => x.Code, ownedBuilder =>
        {
            ownedBuilder.Property(x => x.Value)
                .HasColumnName(nameof(Department.Code))
                .HasMaxLength(DepartmentConstants.DEPARTMENT_CODE_MAX_LENGTH);

            ownedBuilder.HasIndex(x => x.Value)
                .IsUnique();
        });

        builder.OwnsOne(x => x.Name, ownedBuilder =>
        {
            ownedBuilder.Property(x => x.Value)
                .HasColumnName(nameof(Department.Name))
                .HasMaxLength(DepartmentConstants.DEPARTMENT_NAME_MAX_LENGTH);
        });

        builder.OwnsOne(x => x.Description, ownedBuilder =>
        {
            ownedBuilder.Property(x => x.Value)
                .HasColumnName(nameof(Department.Description))
                .HasMaxLength(SharedConstants.DESCRIPTION_MAX_LENGTH);
        });

        builder.Navigation(x => x.Description)
            .IsRequired(false);
    }
}
