using RaccoonLand.Modules.Security.Authentication.Cloning;

namespace RaccoonLand.Modules.Security.Authentication.Tests.Cloning;

public sealed class AuthenticationSchemeOptionsClonerTests
{
    [Fact]
    public void Populate_CopiesWritablePublicProperties()
    {
        var source = new SourceOptions
        {
            Name = "alpha",
            Count = 7,
        };
        var target = new TargetOptions();

        AuthenticationSchemeOptionsCloner.Populate(source, target);

        Assert.Equal("alpha", target.Name);
        Assert.Equal(7, target.Count);
    }

    [Fact]
    public void Populate_IgnoresReadOnlyTargetProperties()
    {
        var source = new SourceOptions { ReadOnlyValue = "from-source" };
        var target = new TargetOptions();

        AuthenticationSchemeOptionsCloner.Populate(source, target);

        Assert.Equal("target-readonly", target.ReadOnlyValue);
    }

    [Fact]
    public void Populate_KeepsNestedObjectReferencesShallow()
    {
        var nested = new NestedOptions { Value = "shared" };
        var source = new SourceOptions { Nested = nested };
        var target = new TargetOptions();

        AuthenticationSchemeOptionsCloner.Populate(source, target);

        Assert.Same(nested, target.Nested);
        Assert.Equal("shared", target.Nested!.Value);
    }

    [Fact]
    public void Populate_WhenTypesIncompatible_ThrowsInvalidOperationException()
    {
        var source = new MismatchSource { Value = "text" };
        var target = new MismatchTarget();

        var ex = Assert.Throws<InvalidOperationException>(
            () => AuthenticationSchemeOptionsCloner.Populate(source, target));

        Assert.Contains("Cannot copy authentication scheme option 'Value'", ex.Message, StringComparison.Ordinal);
        Assert.Contains("not assignable", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Populate_WhenSourceNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => AuthenticationSchemeOptionsCloner.Populate<SourceOptions, TargetOptions>(null!, new TargetOptions()));
    }

    [Fact]
    public void Populate_WhenTargetNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => AuthenticationSchemeOptionsCloner.Populate(new SourceOptions(), (TargetOptions)null!));
    }

    private sealed class NestedOptions
    {
        public string? Value { get; set; }
    }

    private sealed class SourceOptions
    {
        public string? Name { get; set; }
        public int Count { get; set; }
        public NestedOptions? Nested { get; set; }
        public string ReadOnlyValue { get; set; } = "from-source";
    }

    private sealed class TargetOptions
    {
        public string? Name { get; set; }
        public int Count { get; set; }
        public NestedOptions? Nested { get; set; }
        public string ReadOnlyValue { get; } = "target-readonly";
    }

    private sealed class MismatchSource
    {
        public string Value { get; set; } = string.Empty;
    }

    private sealed class MismatchTarget
    {
        public int Value { get; set; }
    }
}
