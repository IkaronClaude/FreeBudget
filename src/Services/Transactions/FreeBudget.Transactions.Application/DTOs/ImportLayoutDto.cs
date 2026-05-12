namespace FreeBudget.Transactions.Application.DTOs;

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
