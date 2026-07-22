using RaccoonLand.Core.Domain.Abstractions;

namespace RaccoonLand.Core.Domain.Tests.Support;

internal sealed class TestMoney : ValueObject
{
    public TestMoney(string currency, decimal amount)
    {
        Currency = currency;
        Amount = amount;
    }

    public string Currency { get; }

    public decimal Amount { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Currency;
        yield return Amount;
    }
}

internal sealed class OtherTestMoney : ValueObject
{
    public OtherTestMoney(string currency, decimal amount)
    {
        Currency = currency;
        Amount = amount;
    }

    public string Currency { get; }

    public decimal Amount { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Currency;
        yield return Amount;
    }
}
