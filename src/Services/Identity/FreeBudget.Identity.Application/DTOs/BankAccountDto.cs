namespace FreeBudget.Identity.Application.DTOs;

public sealed record BankAccountDto(
    Guid Id,
    Guid OwnerUserId,
    string BankType,
    string? Nickname,
    string? ExternalAccountId,
    bool HasApiCredentials,
    Guid? ParentBankAccountId,
    string? CurrencyCode,
    IReadOnlyList<Guid> AccessibleGroupIds);
