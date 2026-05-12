using FluentAssertions;
using FreeBudget.Identity.Domain.Entities;

namespace FreeBudget.Identity.Domain.Tests.Entities;

public class GroupMemberTests
{
    [Fact]
    public void AddMember_sets_member_properties()
    {
        var group = Group.Create("Household", Guid.NewGuid());

        var member = group.AddMember("partner", role: GroupRole.Member);

        member.GroupId.Should().Be(group.Id);
        member.Label.Should().Be("partner");
        member.OwningUserId.Should().BeNull();
        member.Role.Should().Be(GroupRole.Member);
        member.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Rename_updates_label()
    {
        var group = Group.Create("Household", Guid.NewGuid());
        var member = group.AddMember("partner");

        member.Rename("Anna");

        member.Label.Should().Be("Anna");
    }

    [Fact]
    public void LinkToUser_sets_owning_user()
    {
        var group = Group.Create("Household", Guid.NewGuid());
        var member = group.AddMember("partner");
        var newUser = Guid.NewGuid();

        member.LinkToUser(newUser);

        member.OwningUserId.Should().Be(newUser);
    }

    [Fact]
    public void UnlinkFromUser_clears_owning_user()
    {
        var group = Group.Create("Household", Guid.NewGuid());
        var member = group.AddMember("partner", Guid.NewGuid());

        member.UnlinkFromUser();

        member.OwningUserId.Should().BeNull();
    }

    [Fact]
    public void ChangeRole_updates_role()
    {
        var group = Group.Create("Household", Guid.NewGuid());
        var member = group.AddMember("partner", role: GroupRole.Member);

        member.ChangeRole(GroupRole.Admin);

        member.Role.Should().Be(GroupRole.Admin);
    }
}
