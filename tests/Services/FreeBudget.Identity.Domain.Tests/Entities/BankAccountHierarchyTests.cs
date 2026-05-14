using FluentAssertions;
using FreeBudget.Identity.Domain.Entities;
using FreeBudget.Identity.Domain.ValueObjects;

namespace FreeBudget.Identity.Domain.Tests.Entities;

public class BankAccountHierarchyTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();

    [Fact]
    public void CreateParent_has_no_currency_and_no_parent()
    {
        var parent = BankAccount.CreateParent(OwnerId, BankType.Wise, "Wise");

        parent.Nickname.Should().Be("Wise");
        parent.ParentBankAccountId.Should().BeNull();
        parent.CurrencyCode.Should().BeNull();
    }

    [Fact]
    public void CreateChild_carries_parent_id_and_uppercase_currency()
    {
        var parent = BankAccount.CreateParent(OwnerId, BankType.Wise, "Wise");

        var child = BankAccount.CreateChild(parent, "gbp");

        child.ParentBankAccountId.Should().Be(parent.Id);
        child.CurrencyCode.Should().Be("GBP");
        child.OwnerUserId.Should().Be(parent.OwnerUserId);
        child.BankType.Should().Be(parent.BankType);
        child.Nickname.Should().BeNull();
    }

    [Fact]
    public void CreateChild_with_empty_currency_throws()
    {
        var parent = BankAccount.CreateParent(OwnerId, BankType.Wise, "Wise");

        var act = () => BankAccount.CreateChild(parent, "");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateChild_under_another_child_throws()
    {
        var parent = BankAccount.CreateParent(OwnerId, BankType.Wise, "Wise");
        var child = BankAccount.CreateChild(parent, "GBP");

        var act = () => BankAccount.CreateChild(child, "USD");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Standalone_Create_accepts_optional_currency_code()
    {
        var account = BankAccount.Create(OwnerId, BankType.Barclays, "Barclays Personal", "gbp");

        account.ParentBankAccountId.Should().BeNull();
        account.CurrencyCode.Should().Be("GBP");
    }

    [Fact]
    public void Standalone_Create_without_currency_leaves_it_null()
    {
        var account = BankAccount.Create(OwnerId, BankType.Barclays, "Barclays Personal");

        account.CurrencyCode.Should().BeNull();
    }

    [Fact]
    public void Rename_on_child_throws()
    {
        var parent = BankAccount.CreateParent(OwnerId, BankType.Wise, "Wise");
        var child = BankAccount.CreateChild(parent, "GBP");

        var act = () => child.Rename("Anything");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Rename_on_parent_updates_nickname()
    {
        var parent = BankAccount.CreateParent(OwnerId, BankType.Wise, "Wise");

        parent.Rename("Wise Personal");

        parent.Nickname.Should().Be("Wise Personal");
    }
}
