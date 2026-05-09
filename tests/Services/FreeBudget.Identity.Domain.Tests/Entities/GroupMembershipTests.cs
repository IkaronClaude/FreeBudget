using FluentAssertions;
using FreeBudget.Identity.Domain.Entities;

namespace FreeBudget.Identity.Domain.Tests.Entities;

public class GroupMembershipTests
{
    [Fact]
    public void AddMember_sets_membership_properties()
    {
        var group = Group.Create("Household", Guid.NewGuid());
        var memberId = Guid.NewGuid();

        var membership = group.AddMember(memberId, GroupRole.Member);

        membership.GroupId.Should().Be(group.Id);
        membership.UserId.Should().Be(memberId);
        membership.Role.Should().Be(GroupRole.Member);
        membership.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void ChangeRole_updates_role()
    {
        var group = Group.Create("Household", Guid.NewGuid());
        var membership = group.AddMember(Guid.NewGuid(), GroupRole.Member);

        membership.ChangeRole(GroupRole.Admin);

        membership.Role.Should().Be(GroupRole.Admin);
    }
}
