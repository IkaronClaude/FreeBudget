using FluentAssertions;
using FreeBudget.Identity.Infrastructure.Security;

namespace FreeBudget.Identity.Infrastructure.Tests.Security;

public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _hasher = new();

    [Fact]
    public void Hash_produces_non_empty_value_distinct_from_password()
    {
        var hash = _hasher.Hash("hunter2");

        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().NotBe("hunter2");
    }

    [Fact]
    public void Hash_produces_different_values_for_same_password()
    {
        var a = _hasher.Hash("hunter2");
        var b = _hasher.Hash("hunter2");

        a.Should().NotBe(b);
    }

    [Fact]
    public void Verify_returns_true_for_matching_password()
    {
        var hash = _hasher.Hash("hunter2");

        _hasher.Verify("hunter2", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_returns_false_for_mismatched_password()
    {
        var hash = _hasher.Hash("hunter2");

        _hasher.Verify("wrong", hash).Should().BeFalse();
    }

    [Theory]
    [InlineData(null, "some-hash")]
    [InlineData("", "some-hash")]
    [InlineData("   ", "some-hash")]
    [InlineData("pw", null)]
    [InlineData("pw", "")]
    [InlineData("pw", "not-a-bcrypt-hash")]
    public void Verify_returns_false_for_invalid_inputs(string? password, string? hash)
    {
        _hasher.Verify(password!, hash!).Should().BeFalse();
    }
}
