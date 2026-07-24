using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaccoonLand.Modules.Persistence.EntityFrameworkCore.Configuration;

namespace RaccoonLand.Modules.Persistence.EntityFrameworkCore.Tests.Extensions.Fakes;

// Each scanner test uses its own test-marker interface (derived from one of the CQRS markers) so scans
// do not pollute each other. Fake configs live here at file scope so the scanner actually discovers them.

// -------- Happy path --------
public interface IHappyMarker : ICommandEntityConfiguration;

public sealed class HappyEntity
{
    public int Id { get; set; }
}

public sealed class HappyConfig : IEntityTypeConfiguration<HappyEntity>, IHappyMarker
{
    public void Configure(EntityTypeBuilder<HappyEntity> builder) => builder.HasKey(entity => entity.Id);
}

// -------- Marker-mismatch (query-only, must NOT be picked up by a command-side scan) --------
public sealed class QueryOnlyEntity
{
    public int Id { get; set; }
}

public sealed class QueryOnlyConfig : IEntityTypeConfiguration<QueryOnlyEntity>, IQueryEntityConfiguration
{
    public void Configure(EntityTypeBuilder<QueryOnlyEntity> builder) => builder.HasKey(entity => entity.Id);
}

// -------- Abstract / non-public are skipped --------
public interface IAbstractMarker : ICommandEntityConfiguration;

public sealed class AbstractOnlyEntity
{
    public int Id { get; set; }
}

public abstract class AbstractMarkerBase : IEntityTypeConfiguration<AbstractOnlyEntity>, IAbstractMarker
{
    public void Configure(EntityTypeBuilder<AbstractOnlyEntity> builder) => builder.HasKey(entity => entity.Id);
}

// -------- No IEntityTypeConfiguration<> --------
public interface INoConfigMarker : ICommandEntityConfiguration;

public sealed class NoConfigClass : INoConfigMarker;

// -------- Multiple IEntityTypeConfiguration<> on one class --------
public interface IMultiConfigMarker : ICommandEntityConfiguration;

public sealed class MultiEntityA
{
    public int Id { get; set; }
}

public sealed class MultiEntityB
{
    public int Id { get; set; }
}

public sealed class MultiConfig :
    IEntityTypeConfiguration<MultiEntityA>,
    IEntityTypeConfiguration<MultiEntityB>,
    IMultiConfigMarker
{
    public void Configure(EntityTypeBuilder<MultiEntityA> builder) => builder.HasKey(entity => entity.Id);
    public void Configure(EntityTypeBuilder<MultiEntityB> builder) => builder.HasKey(entity => entity.Id);
}

// -------- Dual CQRS markers on one class --------
public interface IDualCqrsMarker : ICommandEntityConfiguration, IQueryEntityConfiguration;

public sealed class DualEntity
{
    public int Id { get; set; }
}

public sealed class DualConfig : IEntityTypeConfiguration<DualEntity>, IDualCqrsMarker
{
    public void Configure(EntityTypeBuilder<DualEntity> builder) => builder.HasKey(entity => entity.Id);
}

// -------- No public parameterless ctor --------
public interface INoCtorMarker : ICommandEntityConfiguration;

public sealed class NoCtorEntity
{
    public int Id { get; set; }
}

public sealed class NoCtorConfig : IEntityTypeConfiguration<NoCtorEntity>, INoCtorMarker
{
    public NoCtorConfig(string _)
    {
    }

    public void Configure(EntityTypeBuilder<NoCtorEntity> builder) => builder.HasKey(entity => entity.Id);
}

// -------- Ctor throws --------
public interface IThrowCtorMarker : ICommandEntityConfiguration;

public sealed class ThrowCtorEntity
{
    public int Id { get; set; }
}

public sealed class ThrowCtorConfig : IEntityTypeConfiguration<ThrowCtorEntity>, IThrowCtorMarker
{
    public ThrowCtorConfig() => throw new InvalidOperationException("ctor-boom");
    public void Configure(EntityTypeBuilder<ThrowCtorEntity> builder) => builder.HasKey(entity => entity.Id);
}

// -------- Configure throws --------
public interface IThrowConfigureMarker : ICommandEntityConfiguration;

public sealed class ThrowConfigureEntity
{
    public int Id { get; set; }
}

public sealed class ThrowConfigureConfig : IEntityTypeConfiguration<ThrowConfigureEntity>, IThrowConfigureMarker
{
    public void Configure(EntityTypeBuilder<ThrowConfigureEntity> builder)
        => throw new InvalidOperationException("configure-boom");
}
