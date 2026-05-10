namespace FreeBudget.Transactions.Application.DTOs;

public sealed record CategoryBreakdownItem(
    string Category,
    decimal TotalCredit,
    decimal TotalDebit,
    decimal Net,
    int TransactionCount);
