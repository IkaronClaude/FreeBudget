namespace FreeBudget.Transactions.Application.DTOs;

public sealed record SharingRuleDto(
    Guid Id,
    string Pattern,
    string MatchType,
    int Priority,
    Guid GroupId,
    Guid PaidByMemberId,
    IReadOnlyList<Guid> ParticipantMemberIds);
