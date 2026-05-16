using FluentAssertions;
using FreeBudget.Identity.Domain.Entities;

namespace FreeBudget.Identity.Domain.Tests.Entities;

public class UserCredentialTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Create_sets_properties()
    {
        var credential = UserCredential.Create(UserId, "hashed-pw");

        credential.UserId.Should().Be(UserId);
        credential.PasswordHash.Should().Be("hashed-pw");
        credential.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_with_empty_userId_throws()
    {
        var act = () => UserCredential.Create(Guid.Empty, "hashed-pw");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_with_invalid_hash_throws(string? hash)
    {
        var act = () => UserCredential.Create(UserId, hash!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateHash_changes_hash()
    {
        var credential = UserCredential.Create(UserId, "old-hash");

        credential.UpdateHash("new-hash");

        credential.PasswordHash.Should().Be("new-hash");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateHash_with_invalid_hash_throws(string? hash)
    {
        var credential = UserCredential.Create(UserId, "old-hash");

        var act = () => credential.UpdateHash(hash!);

        act.Should().Throw<ArgumentException>();
    }
}
