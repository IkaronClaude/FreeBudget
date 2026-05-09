using FluentAssertions;
using FreeBudget.SharedKernel.Domain;

namespace FreeBudget.SharedKernel.Tests;

public class ValueObjectTests
{
    private sealed class Money : ValueObject
    {
        public decimal Amount { get; }
        public string Currency { get; }

        public Money(decimal amount, string currency)
        {
            Amount = amount;
            Currency = currency;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }
    }

    [Fact]
    public void Value_objects_with_same_components_are_equal()
    {
        var a = new Money(10.00m, "GBP");
        var b = new Money(10.00m, "GBP");

        a.Should().Be(b);
        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Value_objects_with_different_components_are_not_equal()
    {
        var a = new Money(10.00m, "GBP");
        var b = new Money(10.00m, "USD");

        a.Should().NotBe(b);
        (a != b).Should().BeTrue();
    }
}
