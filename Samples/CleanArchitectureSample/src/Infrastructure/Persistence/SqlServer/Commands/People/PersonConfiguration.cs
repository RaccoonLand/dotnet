using CleanArchitectureSample.People.Domain.Entities;
using CleanArchitectureSample.People.Shared;
using CleanArchitectureSample.Shared.Localizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaccoonLand.Modules.Persistence.EntityFrameworkCore.Configuration;

namespace CleanArchitectureSample.Infrastructure.Persistence.Commands.People;

public sealed class PersonConfiguration : IEntityTypeConfiguration<Person>, ICommandEntityConfiguration
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("People");

        builder.OwnsOne(x => x.EmployeeCode, buildAction =>
        {
            buildAction.Property(p => p.Value).HasMaxLength(PersonConstants.EMPLOYEE_CODE_MAX_LENGTH).HasColumnName(nameof(Person.EmployeeCode));
            buildAction.HasIndex(p => p.Value).IsUnique();
        });

        builder.OwnsOne(x => x.FirstName, buildAction =>
        {
            buildAction.Property(p => p.Value).HasMaxLength(PersonConstants.FIRST_NAME_MAX_LENGTH).HasColumnName(nameof(Person.FirstName));
        });

        builder.OwnsOne(x => x.LastName, buildAction =>
        {
            buildAction.Property(p => p.Value).HasMaxLength(PersonConstants.LAST_NAME_MAX_LENGTH).HasColumnName(nameof(Person.LastName));
        });

        builder.OwnsOne(x => x.NationalCode, buildAction =>
        {
            buildAction.Property(p => p.Value).HasMaxLength(SharedConstants.NATIONAL_CODE_LENGTH).HasColumnName(nameof(Person.NationalCode));
            buildAction.HasIndex(p => p.Value).IsUnique();
        });

        builder.OwnsOne(x => x.Email, buildAction =>
        {
            buildAction.Property(p => p.Value).HasMaxLength(SharedConstants.EMAIL_MAX_LENGTH).HasColumnName(nameof(Person.Email));
        });

        builder.OwnsOne(x => x.MobileNumber, buildAction =>
        {
            buildAction.Property(p => p.Value).HasMaxLength(SharedConstants.MOBILE_NUMBER_MAX_LENGTH).HasColumnName(nameof(Person.MobileNumber));
        });
    }
}
