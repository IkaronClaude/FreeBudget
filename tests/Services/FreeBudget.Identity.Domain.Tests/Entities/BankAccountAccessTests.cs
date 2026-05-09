using FluentAssertions;
using FreeBudget.Identity.Domain.Entities;
using FreeBudget.Identity.Domain.ValueObjects;

namespace FreeBudget.Identity.Domain.Tests.Entities;

public class BankAccountAccessTests
{
    [Fact]
    public void GrantAccess_sets_properties()
    {
        var account = BankAccount.Create(Guid.NewGuid(), BankType.Wise, "Wise Account");
        var groupId = Guid.NewGuid();

        var access = account.GrantAccessToGroup(groupId);

        access.BankAccountId.Should().Be(account.Id);
        access.GroupId.Should().Be(groupId);
        access.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void GrantedAt_is_set_to_approximately_now()
    {
        var before = DateTime.UtcNow;
        var account = BankAccount.Create(Guid.NewGuid(), BankType.Wise, "Wise Account");

        var access = account.GrantAccessToGroup(Guid.NewGuid());

        access.GrantedAt.Should().BeOnOrAfter(before);
        access.GrantedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }
}
