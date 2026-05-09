using FluentAssertions;
using FreeBudget.Identity.Domain.Entities;
using FreeBudget.Identity.Domain.Events;
using FreeBudget.Identity.Domain.ValueObjects;
using FreeBudget.SharedKernel.Domain;

namespace FreeBudget.Identity.Domain.Tests.Entities;

public class UserTests
{
    [Fact]
    public void Create_sets_properties_correctly()
    {
        var email = Email.Create("anna@example.com");

        var user = User.Create(email, "Anna");

        user.Email.Should().Be(email);
        user.DisplayName.Should().Be("Anna");
    }

    [Fact]
    public void Create_generates_non_empty_id()
    {
        var user = User.Create(Email.Create("anna@example.com"), "Anna");

        user.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_raises_UserCreatedEvent()
    {
        var user = User.Create(Email.Create("anna@example.com"), "Anna");

        user.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserCreatedEvent>()
            .Which.UserId.Should().Be(user.Id);
    }

    [Fact]
    public void Create_with_null_email_throws()
    {
        var act = () => User.Create(null!, "Anna");

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_with_invalid_displayName_throws(string? name)
    {
        var act = () => User.Create(Email.Create("anna@example.com"), name!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateDisplayName_changes_name()
    {
        var user = User.Create(Email.Create("anna@example.com"), "Anna");

        user.UpdateDisplayName("Anna Weber");

        user.DisplayName.Should().Be("Anna Weber");
    }

    [Fact]
    public void UpdateDisplayName_with_empty_throws()
    {
        var user = User.Create(Email.Create("anna@example.com"), "Anna");

        var act = () => user.UpdateDisplayName("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateEmail_changes_email()
    {
        var user = User.Create(Email.Create("anna@example.com"), "Anna");
        var newEmail = Email.Create("anna.weber@example.com");

        user.UpdateEmail(newEmail);

        user.Email.Should().Be(newEmail);
    }

    [Fact]
    public void User_implements_IAuditableEntity()
    {
        var user = User.Create(Email.Create("anna@example.com"), "Anna");

        user.Should().BeAssignableTo<IAuditableEntity>();
    }
}
