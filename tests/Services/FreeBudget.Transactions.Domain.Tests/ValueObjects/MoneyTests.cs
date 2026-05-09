using FluentAssertions;
using FreeBudget.Transactions.Domain.ValueObjects;

namespace FreeBudget.Transactions.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Constructor_sets_amount_and_currency()
    {
        var money = new Money(10.50m, "GBP");

        money.Amount.Should().Be(10.50m);
        money.CurrencyCode.Should().Be("GBP");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Constructor_with_empty_currency_throws(string? currency)
    {
        var act = () => new Money(10m, currency!);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("GB")]
    [InlineData("GBPP")]
    [InlineData("12A")]
    public void Constructor_with_invalid_currency_length_throws(string currency)
    {
        var act = () => new Money(10m, currency);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Currency_is_stored_uppercase()
    {
        var money = new Money(10m, "gbp");

        money.CurrencyCode.Should().Be("GBP");
    }

    [Fact]
    public void Same_money_values_are_equal()
    {
        var a = new Money(10.50m, "GBP");
        var b = new Money(10.50m, "GBP");

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Different_amounts_are_not_equal()
    {
        var a = new Money(10m, "GBP");
        var b = new Money(20m, "GBP");

        a.Should().NotBe(b);
    }

    [Fact]
    public void Different_currencies_are_not_equal()
    {
        var a = new Money(10m, "GBP");
        var b = new Money(10m, "USD");

        a.Should().NotBe(b);
    }

    [Fact]
    public void Add_same_currency_returns_correct_sum()
    {
        var a = new Money(10m, "GBP");
        var b = new Money(5.50m, "GBP");

        var result = a + b;

        result.Amount.Should().Be(15.50m);
        result.CurrencyCode.Should().Be("GBP");
    }

    [Fact]
    public void Add_different_currency_throws()
    {
        var a = new Money(10m, "GBP");
        var b = new Money(5m, "USD");

        var act = () => a + b;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Subtract_same_currency_returns_correct_difference()
    {
        var a = new Money(10m, "GBP");
        var b = new Money(3m, "GBP");

        var result = a - b;

        result.Amount.Should().Be(7m);
    }

    [Fact]
    public void Negate_returns_negated_amount()
    {
        var money = new Money(10m, "GBP");

        var negated = money.Negate();

        negated.Amount.Should().Be(-10m);
        negated.CurrencyCode.Should().Be("GBP");
    }
}
