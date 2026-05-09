using FreeBudget.Identity.Domain.Events;
using FreeBudget.SharedKernel.Domain;

namespace FreeBudget.Identity.Domain.Entities;

public sealed class Group : AggregateRoot<Guid>, IAuditableEntity
{
    private readonly List<GroupMembership> _memberships = [];

    private Group() { }

    public string Name { get; private set; } = null!;
    public Guid CreatedByUserId { get; private init; }
    public IReadOnlyList<GroupMembership> Memberships => _memberships.AsReadOnly();
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }

    public static Group Create(string name, Guid createdByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (createdByUserId == Guid.Empty)
            throw new ArgumentException("Creator user ID cannot be empty.", nameof(createdByUserId));

        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            CreatedByUserId = createdByUserId,
        };

        group._memberships.Add(new GroupMembership(group.Id, createdByUserId, GroupRole.Admin));
        group.RaiseDomainEvent(new GroupCreatedEvent(group.Id, createdByUserId));

        return group;
    }

    public GroupMembership AddMember(Guid userId, GroupRole role = GroupRole.Member)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        if (_memberships.Any(m => m.UserId == userId))
            throw new InvalidOperationException($"User '{userId}' is already a member of this group.");

        var membership = new GroupMembership(Id, userId, role);
        _memberships.Add(membership);

        return membership;
    }

    public void RemoveMember(Guid userId)
    {
        if (userId == CreatedByUserId)
            throw new InvalidOperationException("Cannot remove the group creator.");

        var membership = _memberships.FirstOrDefault(m => m.UserId == userId)
            ?? throw new InvalidOperationException($"User '{userId}' is not a member of this group.");

        _memberships.Remove(membership);
    }

    public void Rename(string newName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);
        Name = newName.Trim();
    }
}
