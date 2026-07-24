using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaccoonLand.Modules.Persistence.EntityFrameworkCore.Configuration;
using RaccoonLand.Modules.Persistence.EntityFrameworkCore.Extensions;
using RaccoonLand.Modules.Persistence.EntityFrameworkCore.Tests.Extensions.Fakes;

namespace RaccoonLand.Modules.Persistence.EntityFrameworkCore.Tests.Extensions;

/// <summary>
/// Each test uses its own private test-marker interface (inheriting from one of the CQRS markers) so
/// scans do not pollute each other despite living in the same assembly. Fake config classes live in the
/// <c>Fakes</c> namespace at file scope and each is tagged with a single test-marker.
/// </summary>
public sealed class ApplyConfigurationsFromAssemblyTests
{
    private static ModelBuilder NewBuilder() => new();

    private static Assembly Self => typeof(ApplyConfigurationsFromAssemblyTests).Assembly;

    [Fact]
    public void ApplyConfigurationsFromAssembly_HappyPath_AppliesMatchingConfig()
    {
        var builder = NewBuilder();

        builder.ApplyConfigurationsFromAssembly<IHappyMarker>(Self);

        var entity = builder.Model.FindEntityType(typeof(HappyEntity));
        Assert.NotNull(entity);
        Assert.NotNull(entity!.FindPrimaryKey());
    }

    [Fact]
    public void ApplyConfigurationsFromAssembly_SkipsTypesNotMatchingMarker()
    {
        var builder = NewBuilder();

        builder.ApplyConfigurationsFromAssembly<IHappyMarker>(Self);

        // QueryOnlyEntity carries a Query-side marker and must NOT be picked up by a Happy (command-side) scan.
        Assert.Null(builder.Model.FindEntityType(typeof(QueryOnlyEntity)));
    }

    [Fact]
    public void ApplyConfigurationsFromAssembly_SkipsAbstractAndNonPublicTypes()
    {
        var builder = NewBuilder();

        builder.ApplyConfigurationsFromAssembly<IAbstractMarker>(Self);

        // AbstractMarkerBase is abstract -> ignored; AbstractOnlyEntity is only configured by an abstract type
        // (which is skipped), so nothing lands in the model.
        Assert.Null(builder.Model.FindEntityType(typeof(AbstractOnlyEntity)));
    }

    [Fact]
    public void ApplyConfigurationsFromAssembly_WhenMarkerWithoutIEntityTypeConfiguration_Throws()
    {
        var builder = NewBuilder();

        var ex = Assert.Throws<InvalidOperationException>(
            () => builder.ApplyConfigurationsFromAssembly<INoConfigMarker>(Self));
        Assert.Contains(nameof(NoConfigClass), ex.Message);
    }

    [Fact]
    public void ApplyConfigurationsFromAssembly_WhenMultipleIEntityTypeConfigurationInterfaces_Throws()
    {
        var builder = NewBuilder();

        var ex = Assert.Throws<InvalidOperationException>(
            () => builder.ApplyConfigurationsFromAssembly<IMultiConfigMarker>(Self));
        Assert.Contains(nameof(MultiConfig), ex.Message);
    }

    [Fact]
    public void ApplyConfigurationsFromAssembly_WhenBothCqrsMarkersOnSameClass_Throws()
    {
        var builder = NewBuilder();

        var ex = Assert.Throws<InvalidOperationException>(
            () => builder.ApplyConfigurationsFromAssembly<IDualCqrsMarker>(Self));
        Assert.Contains(nameof(DualConfig), ex.Message);
    }

    [Fact]
    public void ApplyConfigurationsFromAssembly_WhenNoPublicParameterlessConstructor_Throws()
    {
        var builder = NewBuilder();

        var ex = Assert.Throws<InvalidOperationException>(
            () => builder.ApplyConfigurationsFromAssembly<INoCtorMarker>(Self));
        Assert.Contains("parameterless constructor", ex.Message);
    }

    [Fact]
    public void ApplyConfigurationsFromAssembly_WhenConstructorThrows_WrapsAsInvalidOperationException()
    {
        var builder = NewBuilder();

        var ex = Assert.Throws<InvalidOperationException>(
            () => builder.ApplyConfigurationsFromAssembly<IThrowCtorMarker>(Self));
        Assert.NotNull(ex.InnerException);
    }

    [Fact]
    public void ApplyConfigurationsFromAssembly_WhenConfigureThrows_WrapsAsInvalidOperationException()
    {
        var builder = NewBuilder();

        var ex = Assert.Throws<InvalidOperationException>(
            () => builder.ApplyConfigurationsFromAssembly<IThrowConfigureMarker>(Self));
        Assert.NotNull(ex.InnerException);
    }

    [Fact]
    public void ApplyConfigurationsFromAssembly_WhenModelBuilderNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => ModelBuilderConfigurationExtensions.ApplyConfigurationsFromAssembly<IHappyMarker>(null!, Self));
    }

    [Fact]
    public void ApplyConfigurationsFromAssembly_WhenAssemblyNull_Throws()
    {
        var builder = NewBuilder();

        Assert.Throws<ArgumentNullException>(
            () => builder.ApplyConfigurationsFromAssembly<IHappyMarker>(null!));
    }
}
