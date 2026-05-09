using FluentAssertions;
using FreeBudget.Transactions.Domain.ValueObjects;

namespace FreeBudget.Transactions.Domain.Tests.ValueObjects;

public class TransactionDirectionTests
{
    [Fact]
    public void Credit_has_correct_value()
    {
        TransactionDirection.Credit.Value.Should().Be("Credit");
    }

    [Fact]
    public void Debit_has_correct_value()
    {
        TransactionDirection.Debit.Value.Should().Be("Debit");
    }

    [Theory]
    [InlineData("Credit")]
    [InlineData("Debit")]
    public void From_valid_string_returns_instance(string value)
    {
        var direction = TransactionDirection.From(value);

        direction.Value.Should().Be(value);
    }

    [Theory]
    [InlineData("credit")]
    [InlineData("DEBIT")]
    public void From_is_case_insensitive(string value)
    {
        var act = () => TransactionDirection.From(value);

        act.Should().NotThrow();
    }

    [Fact]
    public void From_unknown_throws()
    {
        var act = () => TransactionDirection.From("Unknown");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Same_directions_are_equal()
    {
        var a = TransactionDirection.Credit;
        var b = TransactionDirection.From("Credit");

        a.Should().Be(b);
    }

    [Fact]
    public void Different_directions_are_not_equal()
    {
        TransactionDirection.Credit.Should().NotBe(TransactionDirection.Debit);
    }
}
