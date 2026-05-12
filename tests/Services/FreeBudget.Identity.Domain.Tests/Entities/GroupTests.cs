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
    public void Create_adds_creator_as_admin_member_with_default_label()
    {
        var group = Group.Create("Household", CreatorId);

        group.Members.Should().ContainSingle()
            .Which.Should().Match<GroupMember>(m =>
                m.OwningUserId == CreatorId
                && m.Role == GroupRole.Admin
                && m.Label == "me");
    }

    [Fact]
    public void Create_with_custom_creator_label()
    {
        var group = Group.Create("Household", CreatorId, "alice");

        group.Members.Should().ContainSingle()
            .Which.Label.Should().Be("alice");
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
    public void AddMember_with_label_only_creates_placeholder()
    {
        var group = Group.Create("Household", CreatorId);

        var member = group.AddMember("partner");

        member.Label.Should().Be("partner");
        member.OwningUserId.Should().BeNull();
        member.Role.Should().Be(GroupRole.Member);
        group.Members.Should().HaveCount(2);
    }

    [Fact]
    public void AddMember_with_owning_user_links_immediately()
    {
        var group = Group.Create("Household", CreatorId);
        var partnerId = Guid.NewGuid();

        var member = group.AddMember("partner", partnerId);

        member.OwningUserId.Should().Be(partnerId);
    }

    [Fact]
    public void AddMember_duplicate_label_throws()
    {
        var group = Group.Create("Household", CreatorId);
        group.AddMember("partner");

        var act = () => group.AddMember("partner");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddMember_duplicate_owning_user_throws()
    {
        var group = Group.Create("Household", CreatorId);
        var partnerId = Guid.NewGuid();
        group.AddMember("partner", partnerId);

        var act = () => group.AddMember("partner-2", partnerId);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RemoveMember_removes_by_id()
    {
        var group = Group.Create("Household", CreatorId);
        var added = group.AddMember("partner");

        group.RemoveMember(added.Id);

        group.Members.Should().ContainSingle()
            .Which.OwningUserId.Should().Be(CreatorId);
    }

    [Fact]
    public void RemoveMember_creator_throws()
    {
        var group = Group.Create("Household", CreatorId);
        var creatorMember = group.Members.Single();

        var act = () => group.RemoveMember(creatorMember.Id);

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
