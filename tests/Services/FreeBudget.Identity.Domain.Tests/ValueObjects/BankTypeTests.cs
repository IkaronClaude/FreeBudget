using FluentAssertions;
using FreeBudget.Identity.Domain.ValueObjects;

namespace FreeBudget.Identity.Domain.Tests.ValueObjects;

public class BankTypeTests
{
    [Fact]
    public void Barclays_has_correct_name()
    {
        BankType.Barclays.Name.Should().Be("Barclays");
    }

    [Fact]
    public void Wise_has_correct_name()
    {
        BankType.Wise.Name.Should().Be("Wise");
    }

    [Fact]
    public void NatWest_has_correct_name()
    {
        BankType.NatWest.Name.Should().Be("NatWest");
    }

    [Theory]
    [InlineData("Barclays")]
    [InlineData("Wise")]
    [InlineData("NatWest")]
    public void From_valid_name_returns_correct_instance(string name)
    {
        var bankType = BankType.From(name);

        bankType.Name.Should().Be(name);
    }

    [Theory]
    [InlineData("barclays")]
    [InlineData("WISE")]
    [InlineData("natwest")]
    public void From_is_case_insensitive(string name)
    {
        var act = () => BankType.From(name);

        act.Should().NotThrow();
    }

    [Fact]
    public void From_unknown_name_throws()
    {
        var act = () => BankType.From("UnknownBank");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Same_bank_types_are_equal()
    {
        var a = BankType.Barclays;
        var b = BankType.From("Barclays");

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Different_bank_types_are_not_equal()
    {
        BankType.Barclays.Should().NotBe(BankType.Wise);
    }
}
