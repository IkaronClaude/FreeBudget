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
        DateColumn = "Date",
        DescriptionColumn = "Description",
        AmountColumn = "Amount",
        CurrencyColumn = "Currency",
        DirectionColumn = "Direction",
        DateFormat = "dd-MM-yyyy",
        DefaultCurrencyCode = "GBP",
        CreatedByUserId = createdByUserId,
    };
}
