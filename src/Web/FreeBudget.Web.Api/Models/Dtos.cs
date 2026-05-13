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

public sealed record CreateBankAccountDto(string BankType, string Nickname);
public sealed record RenameBankAccountDto(string Nickname);

public sealed record CreateGroupDto(string Name, string? CreatorLabel);
public sealed record RenameGroupDto(string Name);
public sealed record AddGroupMemberDto(string Label, Guid? OwningUserId);
public sealed record RenameGroupMemberDto(string Label);
public sealed record LinkGroupMemberDto(Guid OwningUserId);

public sealed record ImportLayoutDto(
    Guid? Id,
    Guid BankAccountId,
    string Name,
    string DateColumn,
    string DescriptionColumn,
    string AmountColumn,
    string? CurrencyColumn,
    string? DirectionColumn,
    Dictionary<string, string>? DirectionMappings,
    string? ExternalIdColumn,
    string? RunningBalanceColumn,
    string? CategoryColumn,
    string DateFormat,
    bool HasHeaderRow,
    string Delimiter,
    string DefaultCurrencyCode);

public sealed record SplitParticipantInputDto(Guid MemberId, decimal Amount);

public sealed record SplitTransactionInputDto(
    Guid GroupId,
    Guid PaidByMemberId,
    Guid TransactionId,
    string CurrencyCode,
    string Description,
    DateTime EntryDate,
    IReadOnlyList<SplitParticipantInputDto> Participants);

public sealed record CreateLedgerEntryInputDto(
    Guid GroupId,
    Guid PaidByMemberId,
    Guid OwedByMemberId,
    decimal Amount,
    string CurrencyCode,
    string Description,
    DateTime EntryDate,
    Guid? TransactionId);

public sealed record SharingRuleDto(
    Guid Id,
    string Pattern,
    string MatchType,
    string EntryType,
    int Priority,
    Guid GroupId,
    Guid PaidByMemberId,
    IReadOnlyList<Guid> ParticipantMemberIds);

public sealed record CreateSharingRuleInputDto(
    string Pattern,
    string MatchType,
    string? EntryType,
    int Priority,
    Guid GroupId,
    Guid PaidByMemberId,
    IReadOnlyList<Guid> ParticipantMemberIds);

public sealed record UpdateSharingRuleInputDto(
    string Pattern,
    string MatchType,
    string? EntryType,
    int Priority,
    Guid GroupId,
    Guid PaidByMemberId,
    IReadOnlyList<Guid> ParticipantMemberIds);

public sealed record ApplySharingRulesResult(int Examined, int Matched, int Split, int Skipped, int TransfersPaired);

public sealed record MatchTransfersResultDto(int Examined, int Matched, int AmbiguousSkipped);

public sealed record UpsertImportLayoutInputDto(
    string Name,
    string DateColumn,
    string DescriptionColumn,
    string AmountColumn,
    string? CurrencyColumn,
    string? DirectionColumn,
    Dictionary<string, string>? DirectionMappings,
    string? ExternalIdColumn,
    string? RunningBalanceColumn,
    string? CategoryColumn,
    string DateFormat,
    bool HasHeaderRow,
    string Delimiter,
    string DefaultCurrencyCode);
