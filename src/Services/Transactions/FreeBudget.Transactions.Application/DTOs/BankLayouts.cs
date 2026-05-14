namespace FreeBudget.Transactions.Application.DTOs;

public static class BankLayouts
{
    public static ImportLayout Barclays(Guid createdByUserId) => new()
    {
        Name = "Barclays",
        BankTypeHint = "Barclays",
        DateColumn = "Date",
        DescriptionColumn = "Memo",
        AmountColumn = "Amount",
        DateFormat = "dd/MM/yyyy",
        DefaultCurrencyCode = "GBP",
        CreatedByUserId = createdByUserId,
    };

    public static ImportLayout Wise(Guid createdByUserId) => new()
    {
        Name = "Wise",
        BankTypeHint = "Wise",
        DateColumn = "Created on",
        DescriptionColumn = "Target name",
        AmountColumn = "Source amount (after fees)",
        CurrencyColumn = "Source currency",
        DirectionColumn = "Direction",
        DirectionMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["IN"] = "Credit",
            ["OUT"] = "Debit",
            ["NEUTRAL"] = "Neutral",
        },
        ExternalIdColumn = "ID",
        CategoryColumn = "Category",
        TargetAmountColumn = "Target amount (after fees)",
        TargetCurrencyColumn = "Target currency",
        DateFormat = "dd/MM/yyyy HH:mm",
        DefaultCurrencyCode = "GBP",
        CreatedByUserId = createdByUserId,
    };
}
