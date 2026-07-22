using RaccoonLand.Core.Domain.Exceptions;

namespace RaccoonLand.Core.Domain.Tests.Exceptions;

public sealed class DomainExceptionTests
{
    [Fact]
    public void SingleErrorConstructor_CreatesExactlyOneDomainError()
    {
        var exception = new DomainException("E001", "order.invalid", 42);

        var error = Assert.Single(exception.Errors);
        Assert.Equal("E001", error.Code);
        Assert.Equal("order.invalid", error.Message);
        Assert.Equal(42, Assert.Single(error.Parameters));
    }

    [Fact]
    public void Constructor_Throws_WhenErrorsIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new DomainException(errors: null!));
    }

    [Fact]
    public void Constructor_Throws_WhenErrorsIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => new DomainException(errors: []));
    }

    [Fact]
    public void Constructor_ThrowsNullReferenceException_WhenAnErrorElementIsNull()
    {
        // Current contract: null elements are invalid input. Message composition
        // dereferences each error, so a null element fails with NullReferenceException.
        // This documents today's behavior; a stricter ArgumentException may replace it later.
        var valid = new DomainError("E1", "first");

        Assert.Throws<NullReferenceException>(() => new DomainException(valid, null!));
    }

    [Fact]
    public void Constructor_PreservesErrorOrder()
    {
        var first = new DomainError("E1", "first");
        var second = new DomainError("E2", "second");
        var third = new DomainError("E3", "third");

        var exception = new DomainException(first, second, third);

        Assert.Equal([first, second, third], exception.Errors);
    }

    [Fact]
    public void Message_JoinsErrorMessagesWithSemicolonSeparator()
    {
        var exception = new DomainException(
            new DomainError("E1", "alpha"),
            new DomainError("E2", "beta"),
            new DomainError("E3", "gamma"));

        Assert.Equal("alpha; beta; gamma", exception.Message);
    }
}
