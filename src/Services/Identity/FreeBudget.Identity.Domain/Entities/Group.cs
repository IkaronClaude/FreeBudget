using FreeBudget.Identity.Domain.Events;
using FreeBudget.SharedKernel.Domain;

namespace FreeBudget.Identity.Domain.Entities;

public sealed class Group : AggregateRoot<Guid>, IAuditableEntity
{
    private readonly List<GroupMember> _members = [];

    private Group() { }

    public string Name { get; private set; } = null!;
    public Guid CreatedByUserId { get; private init; }
    public IReadOnlyList<GroupMember> Members => _members.AsReadOnly();
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }

    public static Group Create(string name, Guid createdByUserId, string creatorLabel = "me")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(creatorLabel);

        if (createdByUserId == Guid.Empty)
            throw new ArgumentException("Creator user ID cannot be empty.", nameof(createdByUserId));

        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            CreatedByUserId = createdByUserId,
        };

        group._members.Add(new GroupMember(group.Id, creatorLabel, createdByUserId, GroupRole.Admin));
        group.RaiseDomainEvent(new GroupCreatedEvent(group.Id, createdByUserId));

        return group;
    }

    public GroupMember AddMember(string label, Guid? owningUserId = null, GroupRole role = GroupRole.Member)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);

        if (owningUserId.HasValue && _members.Any(m => m.OwningUserId == owningUserId.Value))
            throw new InvalidOperationException(
                $"User '{owningUserId}' is already linked to a member of this group.");

        if (_members.Any(m => string.Equals(m.Label, label.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException(
                $"A member with label '{label}' already exists in this group.");

        var member = new GroupMember(Id, label, owningUserId, role);
        _members.Add(member);

        return member;
    }

    public void RemoveMember(Guid memberId)
    {
        var member = _members.FirstOrDefault(m => m.Id == memberId)
            ?? throw new InvalidOperationException($"Member '{memberId}' is not in this group.");

        if (member.OwningUserId == CreatedByUserId)
            throw new InvalidOperationException("Cannot remove the group creator.");

        _members.Remove(member);
    }

    public void Rename(string newName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);
        Name = newName.Trim();
    }
}
