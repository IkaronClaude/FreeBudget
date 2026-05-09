using FluentAssertions;
using FreeBudget.Identity.Domain.Entities;
using FreeBudget.Identity.Domain.Events;

namespace FreeBudget.Identity.Domain.Tests.Entities;

public class GroupTests
{
    private static readonly Guid CreatorId = Guid.NewGuid();

    [Fact]
    public void Create_sets_properties_correctly()
    {
        var group = Group.Create("Household", CreatorId);

        group.Name.Should().Be("Household");
        group.CreatedByUserId.Should().Be(CreatorId);
        group.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_adds_creator_as_admin_member()
    {
        var group = Group.Create("Household", CreatorId);

        group.Memberships.Should().ContainSingle()
            .Which.Should().Match<GroupMembership>(m =>
                m.UserId == CreatorId && m.Role == GroupRole.Admin);
    }

    [Fact]
    public void Create_raises_GroupCreatedEvent()
    {
        var group = Group.Create("Household", CreatorId);

        group.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<GroupCreatedEvent>()
            .Which.GroupId.Should().Be(group.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_with_invalid_name_throws(string? name)
    {
        var act = () => Group.Create(name!, CreatorId);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_with_empty_userId_throws()
    {
        var act = () => Group.Create("Household", Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddMember_adds_membership()
    {
        var group = Group.Create("Household", CreatorId);
        var memberId = Guid.NewGuid();

        group.AddMember(memberId);

        group.Memberships.Should().HaveCount(2);
        group.Memberships.Should().Contain(m => m.UserId == memberId && m.Role == GroupRole.Member);
    }

    [Fact]
    public void AddMember_returns_created_membership()
    {
        var group = Group.Create("Household", CreatorId);
        var memberId = Guid.NewGuid();

        var membership = group.AddMember(memberId, GroupRole.Admin);

        membership.UserId.Should().Be(memberId);
        membership.Role.Should().Be(GroupRole.Admin);
    }

    [Fact]
    public void AddMember_duplicate_userId_throws()
    {
        var group = Group.Create("Household", CreatorId);
        var memberId = Guid.NewGuid();
        group.AddMember(memberId);

        var act = () => group.AddMember(memberId);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RemoveMember_removes_membership()
    {
        var group = Group.Create("Household", CreatorId);
        var memberId = Guid.NewGuid();
        group.AddMember(memberId);

        group.RemoveMember(memberId);

        group.Memberships.Should().ContainSingle()
            .Which.UserId.Should().Be(CreatorId);
    }

    [Fact]
    public void RemoveMember_creator_throws()
    {
        var group = Group.Create("Household", CreatorId);

        var act = () => group.RemoveMember(CreatorId);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RemoveMember_nonexistent_throws()
    {
        var group = Group.Create("Household", CreatorId);

        var act = () => group.RemoveMember(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Rename_changes_name()
    {
        var group = Group.Create("Household", CreatorId);

        group.Rename("Family Budget");

        group.Name.Should().Be("Family Budget");
    }

    [Fact]
    public void Rename_with_empty_name_throws()
    {
        var group = Group.Create("Household", CreatorId);

        var act = () => group.Rename("");

        act.Should().Throw<ArgumentException>();
    }
}
