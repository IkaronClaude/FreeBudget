namespace FreeBudget.Identity.Application.DTOs;

public sealed record GroupDto(
    Guid Id,
    string Name,
    Guid CreatedByUserId,
    IReadOnlyList<GroupMemberDto> Members);

public sealed record GroupMemberDto(
    Guid Id,
    Guid GroupId,
    string Label,
    Guid? OwningUserId,
    string Role);
