using RaccoonLand.Core.Domain.Exceptions;

namespace RaccoonLand.Core.Domain.Tests.Exceptions;

public sealed class DomainErrorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Constructor_Throws_WhenCodeIsNullOrWhiteSpace(string? code)
    {
        Assert.ThrowsAny<ArgumentException>(() => new DomainError(code!, "message"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Constructor_Throws_WhenMessageIsNullOrWhiteSpace(string? message)
    {
        Assert.ThrowsAny<ArgumentException>(() => new DomainError("code", message!));
    }

    [Fact]
    public void Parameters_IsNeverNull_WhenNoParametersProvided()
    {
        var error = new DomainError("code", "message");

        Assert.NotNull(error.Parameters);
        Assert.Empty(error.Parameters);
    }

    [Fact]
    public void Parameters_IsEmpty_WhenParamsArrayIsExplicitlyNull()
    {
        // Distinct from omitting params: an explicit null array is a different C# binding.
        object?[]? parameters = null;

        var error = new DomainError("code", "message", parameters!);

        Assert.NotNull(error.Parameters);
        Assert.Empty(error.Parameters);
    }

    [Fact]
    public void Parameters_IsDefensiveCopy_AndInputMutationDoesNotAffectInstance()
    {
        object?[] input = ["a", 1];
        var error = new DomainError("code", "message", input);

        input[0] = "mutated";

        Assert.Equal(2, error.Parameters.Count);
        Assert.Equal("a", error.Parameters[0]);
        Assert.Equal(1, error.Parameters[1]);
    }
}
