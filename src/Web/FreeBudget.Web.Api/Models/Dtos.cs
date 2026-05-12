namespace FreeBudget.Web.Api.Models;

public sealed record UserDto(Guid Id, string Email, string DisplayName);

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

public sealed record BankAccountDto(
    Guid Id,
    Guid OwnerUserId,
    string BankType,
    string Nickname,
    string? ExternalAccountId,
    bool HasApiCredentials);

public sealed record MeResponse(
    UserDto User,
    IReadOnlyList<GroupDto> Groups,
    IReadOnlyList<BankAccountDto> BankAccounts);

public sealed record TransactionListItem(
    Guid Id,
    Guid BankAccountId,
    DateTime TransactionDate,
    string Description,
    decimal Amount,
    string CurrencyCode,
    string Direction,
    string? Category,
    string? ExternalTransactionId);

public sealed record CategoryBreakdownItem(
    string Category,
    decimal TotalCredit,
    decimal TotalDebit,
    decimal Net,
    int TransactionCount);

public sealed record PeriodBreakdownItem(
    string PeriodLabel,
    DateTime PeriodStart,
    decimal TotalCredit,
    decimal TotalDebit,
    decimal Net,
    int TransactionCount);

public sealed record ImportCsvResponse(Guid ImportBatchId, int TransactionCount, int SkippedDuplicates);

public sealed record CategorizationRuleDto(
    Guid Id,
    string Pattern,
    string MatchType,
    string Category,
    int Priority);

public sealed record CreateCategorizationRuleRequest(
    string Pattern,
    string MatchType,
    string Category,
    int Priority);

public sealed record UpdateCategorizationRuleRequest(
    string Pattern,
    string MatchType,
    string Category,
    int Priority);

public sealed record UpdateCategoryDto(string? Category);
