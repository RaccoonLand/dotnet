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
    public void Constructor_Throws_WhenAnErrorElementIsNull()
    {
        // Null elements are invalid input; the ctor validates up front so consumers never observe
        // a NullReferenceException from message composition (which would look like an internal bug).
        var valid = new DomainError("E1", "first");

        var ex = Assert.Throws<ArgumentException>(() => new DomainException(valid, null!));

        Assert.Equal("errors", ex.ParamName);
        // Index of the offending element is included so callers can locate the bad argument.
        Assert.Contains("errors[1]", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_Throws_WhenFirstErrorElementIsNull()
    {
        var ex = Assert.Throws<ArgumentException>(() => new DomainException(errors: [null!]));

        Assert.Equal("errors", ex.ParamName);
        Assert.Contains("errors[0]", ex.Message, StringComparison.Ordinal);
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
