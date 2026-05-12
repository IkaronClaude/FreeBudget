using FreeBudget.SharedKernel.Domain;

namespace FreeBudget.Identity.Domain.Entities;

public sealed class GroupMember : Entity<Guid>
{
    private GroupMember() { }

    internal GroupMember(Guid groupId, string label, Guid? owningUserId, GroupRole role)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);

        Id = Guid.NewGuid();
        GroupId = groupId;
        Label = label.Trim();
        OwningUserId = owningUserId;
        Role = role;
    }

    public Guid GroupId { get; private init; }
    public string Label { get; private set; } = null!;
    public Guid? OwningUserId { get; private set; }
    public GroupRole Role { get; private set; }

    public void Rename(string newLabel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newLabel);
        Label = newLabel.Trim();
    }

    public void LinkToUser(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        OwningUserId = userId;
    }

    public void UnlinkFromUser() => OwningUserId = null;

    public void ChangeRole(GroupRole newRole) => Role = newRole;
}
