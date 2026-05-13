namespace FreeBudget.Transactions.Application.DTOs;

public sealed record ImportLayout
{
    public required string Name { get; init; }
    public string? BankTypeHint { get; init; }
    public required string DateColumn { get; init; }
    public required string DescriptionColumn { get; init; }
    public required string AmountColumn { get; init; }
    public string? CurrencyColumn { get; init; }
    public string? DirectionColumn { get; init; }
    public IReadOnlyDictionary<string, string>? DirectionMappings { get; init; }
    public string? ExternalIdColumn { get; init; }
    public string? RunningBalanceColumn { get; init; }
    public string? CategoryColumn { get; init; }
    public string? TargetAmountColumn { get; init; }
    public string? TargetCurrencyColumn { get; init; }
    public string DateFormat { get; init; } = "dd/MM/yyyy";
    public bool HasHeaderRow { get; init; } = true;
    public char Delimiter { get; init; } = ',';
    public string DefaultCurrencyCode { get; init; } = "GBP";
    public Guid CreatedByUserId { get; init; }
}
