using FluentAssertions;
using FreeBudget.Identity.Domain.Entities;
using FreeBudget.Identity.Domain.Events;
using FreeBudget.Identity.Domain.ValueObjects;

namespace FreeBudget.Identity.Domain.Tests.Entities;

public class BankAccountTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();

    private static BankAccount CreateAccount() =>
        BankAccount.Create(OwnerId, BankType.Barclays, "Main Account");

    [Fact]
    public void Create_sets_properties_correctly()
    {
        var account = CreateAccount();

        account.OwnerUserId.Should().Be(OwnerId);
        account.BankType.Should().Be(BankType.Barclays);
        account.Nickname.Should().Be("Main Account");
        account.Id.Should().NotBe(Guid.Empty);
        account.HasApiCredentials.Should().BeFalse();
        account.ExternalAccountId.Should().BeNull();
    }

    [Fact]
    public void Create_raises_BankAccountLinkedEvent()
    {
        var account = CreateAccount();

        account.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<BankAccountLinkedEvent>()
            .Which.BankAccountId.Should().Be(account.Id);
    }

    [Fact]
    public void Create_with_empty_nickname_throws()
    {
        var act = () => BankAccount.Create(OwnerId, BankType.Barclays, "");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_with_empty_ownerId_throws()
    {
        var act = () => BankAccount.Create(Guid.Empty, BankType.Barclays, "Main");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetExternalAccountId_sets_value()
    {
        var account = CreateAccount();

        account.SetExternalAccountId("ACC-123");

        account.ExternalAccountId.Should().Be("ACC-123");
    }

    [Fact]
    public void MarkApiCredentialsConfigured_sets_flag_true()
    {
        var account = CreateAccount();

        account.MarkApiCredentialsConfigured();

        account.HasApiCredentials.Should().BeTrue();
    }

    [Fact]
    public void MarkApiCredentialsRemoved_sets_flag_false()
    {
        var account = CreateAccount();
        account.MarkApiCredentialsConfigured();

        account.MarkApiCredentialsRemoved();

        account.HasApiCredentials.Should().BeFalse();
    }

    [Fact]
    public void Rename_changes_nickname()
    {
        var account = CreateAccount();

        account.Rename("Savings");

        account.Nickname.Should().Be("Savings");
    }

    [Fact]
    public void GrantAccessToGroup_adds_access_grant()
    {
        var account = CreateAccount();
        var groupId = Guid.NewGuid();

        account.GrantAccessToGroup(groupId);

        account.AccessGrants.Should().ContainSingle()
            .Which.GroupId.Should().Be(groupId);
    }

    [Fact]
    public void GrantAccessToGroup_raises_event()
    {
        var account = CreateAccount();
        account.ClearDomainEvents();
        var groupId = Guid.NewGuid();

        account.GrantAccessToGroup(groupId);

        account.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<BankAccountAccessGrantedEvent>();
    }

    [Fact]
    public void GrantAccessToGroup_duplicate_throws()
    {
        var account = CreateAccount();
        var groupId = Guid.NewGuid();
        account.GrantAccessToGroup(groupId);

        var act = () => account.GrantAccessToGroup(groupId);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RevokeAccessFromGroup_removes_grant()
    {
        var account = CreateAccount();
        var groupId = Guid.NewGuid();
        account.GrantAccessToGroup(groupId);

        account.RevokeAccessFromGroup(groupId);

        account.AccessGrants.Should().BeEmpty();
    }

    [Fact]
    public void RevokeAccessFromGroup_unknown_groupId_throws()
    {
        var account = CreateAccount();

        var act = () => account.RevokeAccessFromGroup(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>();
    }
}
