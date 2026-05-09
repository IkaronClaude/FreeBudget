using FreeBudget.SharedKernel.Domain;

namespace FreeBudget.Identity.Domain.Entities;

public sealed class GroupMembership : Entity<Guid>
{
    private GroupMembership() { }

    internal GroupMembership(Guid groupId, Guid userId, GroupRole role)
    {
        Id = Guid.NewGuid();
        GroupId = groupId;
        UserId = userId;
        Role = role;
    }

    public Guid GroupId { get; private init; }
    public Guid UserId { get; private init; }
    public GroupRole Role { get; private set; }

    public void ChangeRole(GroupRole newRole)
    {
        Role = newRole;
    }
}
