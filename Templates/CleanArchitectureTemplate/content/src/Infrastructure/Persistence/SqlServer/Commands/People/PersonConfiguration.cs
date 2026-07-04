using CleanArchitectureTemplate.People.Domain.Entities;
using CleanArchitectureTemplate.People.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaccoonLand.Modules.Persistence.EntityFrameworkCore.Configuration;

namespace CleanArchitectureTemplate.Infrastructure.Persistence.Commands.People;

public sealed class PersonConfiguration : IEntityTypeConfiguration<Person>, ICommandEntityConfiguration
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("Persons");

        builder.OwnsOne(x => x.FirstName, buildAction =>
        {
            buildAction.Property(p => p.Value)
                .HasMaxLength(PersonConstants.FIRST_NAME_MAX_LENGTH)
                .HasColumnName(nameof(Person.FirstName));
        });

        builder.OwnsOne(x => x.LastName, buildAction =>
        {
            buildAction.Property(p => p.Value)
                .HasMaxLength(PersonConstants.LAST_NAME_MAX_LENGTH)
                .HasColumnName(nameof(Person.LastName));
        });
    }
}
