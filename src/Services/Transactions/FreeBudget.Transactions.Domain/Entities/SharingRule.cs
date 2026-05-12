using FreeBudget.SharedKernel.Domain;
using FreeBudget.Transactions.Domain.Enums;

namespace FreeBudget.Transactions.Domain.Entities;

public sealed class SharingRule : Entity<Guid>, IAuditableEntity
{
    private SharingRule() { }

    public Guid CreatedByUserId { get; private init; }
    public string Pattern { get; private set; } = null!;
    public RuleMatchType RuleMatchType { get; private set; }
    public int Priority { get; private set; }
    public Guid GroupId { get; private set; }
    public Guid PaidByMemberId { get; private set; }
    public IReadOnlyList<Guid> ParticipantMemberIds { get; private set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }

    public static SharingRule Create(
        Guid createdByUserId,
        string pattern,
        RuleMatchType matchType,
        Guid groupId,
        Guid paidByMemberId,
        IReadOnlyList<Guid> participantMemberIds,
        int priority = 0)
    {
        if (createdByUserId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(createdByUserId));
        if (groupId == Guid.Empty)
            throw new ArgumentException("Group ID cannot be empty.", nameof(groupId));
        if (paidByMemberId == Guid.Empty)
            throw new ArgumentException("Payer member ID cannot be empty.", nameof(paidByMemberId));
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
        ArgumentNullException.ThrowIfNull(participantMemberIds);
        if (participantMemberIds.Count == 0)
            throw new ArgumentException("At least one participant is required.", nameof(participantMemberIds));
        if (participantMemberIds.Any(id => id == Guid.Empty))
            throw new ArgumentException("Participant member IDs cannot be empty.", nameof(participantMemberIds));
        if (participantMemberIds.Distinct().Count() != participantMemberIds.Count)
            throw new ArgumentException("Participant member IDs must be unique.", nameof(participantMemberIds));

        return new SharingRule
        {
            Id = Guid.NewGuid(),
            CreatedByUserId = createdByUserId,
            Pattern = pattern.Trim(),
            RuleMatchType = matchType,
            Priority = priority,
            GroupId = groupId,
            PaidByMemberId = paidByMemberId,
            ParticipantMemberIds = participantMemberIds.ToList(),
        };
    }

    public void Update(
        string pattern,
        RuleMatchType matchType,
        Guid groupId,
        Guid paidByMemberId,
        IReadOnlyList<Guid> participantMemberIds,
        int priority)
    {
        if (groupId == Guid.Empty)
            throw new ArgumentException("Group ID cannot be empty.", nameof(groupId));
        if (paidByMemberId == Guid.Empty)
            throw new ArgumentException("Payer member ID cannot be empty.", nameof(paidByMemberId));
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
        ArgumentNullException.ThrowIfNull(participantMemberIds);
        if (participantMemberIds.Count == 0)
            throw new ArgumentException("At least one participant is required.", nameof(participantMemberIds));

        Pattern = pattern.Trim();
        RuleMatchType = matchType;
        GroupId = groupId;
        PaidByMemberId = paidByMemberId;
        ParticipantMemberIds = participantMemberIds.ToList();
        Priority = priority;
    }

    public bool Matches(string description)
    {
        return RuleMatchType switch
        {
            RuleMatchType.Contains => description.Contains(Pattern, StringComparison.OrdinalIgnoreCase),
            RuleMatchType.Exact => description.Equals(Pattern, StringComparison.OrdinalIgnoreCase),
            RuleMatchType.StartsWith => description.StartsWith(Pattern, StringComparison.OrdinalIgnoreCase),
            RuleMatchType.EndsWith => description.EndsWith(Pattern, StringComparison.OrdinalIgnoreCase),
            _ => false,
        };
    }
}
