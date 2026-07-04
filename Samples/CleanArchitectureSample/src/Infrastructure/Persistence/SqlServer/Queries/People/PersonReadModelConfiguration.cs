using CleanArchitectureSample.Application.People.Queries.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaccoonLand.Modules.Persistence.EntityFrameworkCore.Configuration;

namespace CleanArchitectureSample.Infrastructure.Persistence.Queries.People;

public sealed class PersonReadModelConfiguration
    : IEntityTypeConfiguration<PersonReadModel>, IQueryEntityConfiguration
{
    public void Configure(EntityTypeBuilder<PersonReadModel> builder)
    {
        builder.ToTable("People");
    }
}
