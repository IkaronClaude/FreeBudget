using FluentAssertions;
using FreeBudget.Identity.Domain.ValueObjects;

namespace FreeBudget.Identity.Domain.Tests.ValueObjects;

public class EmailTests
{
    [Fact]
    public void Create_with_valid_email_succeeds()
    {
        var email = Email.Create("user@example.com");

        email.Value.Should().Be("user@example.com");
    }

    [Fact]
    public void Create_trims_and_lowercases()
    {
        var email = Email.Create("  FOO@BAR.COM  ");

        email.Value.Should().Be("foo@bar.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_with_empty_or_null_throws(string? value)
    {
        var act = () => Email.Create(value!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_with_no_at_sign_throws()
    {
        var act = () => Email.Create("invalid");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_with_no_domain_throws()
    {
        var act = () => Email.Create("user@");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Emails_with_same_value_are_equal()
    {
        var a = Email.Create("user@example.com");
        var b = Email.Create("user@example.com");

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Emails_with_different_values_are_not_equal()
    {
        var a = Email.Create("user@example.com");
        var b = Email.Create("other@example.com");

        a.Should().NotBe(b);
    }

    [Fact]
    public void Implicit_conversion_to_string()
    {
        var email = Email.Create("user@example.com");

        string value = email;

        value.Should().Be("user@example.com");
    }
}
