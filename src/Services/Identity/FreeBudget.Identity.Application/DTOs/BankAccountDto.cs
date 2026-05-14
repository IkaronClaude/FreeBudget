namespace FreeBudget.Identity.Application.DTOs;

public sealed record BankAccountDto(
    Guid Id,
    Guid OwnerUserId,
    string BankType,
    string? Nickname,
    string? ExternalAccountId,
    bool HasApiCredentials,
    IReadOnlyList<Guid> AccessibleGroupIds);
