namespace FreeBudget.Transactions.Application.DTOs;

public sealed record PeriodBreakdownItem(
    string PeriodLabel,
    DateTime PeriodStart,
    decimal TotalCredit,
    decimal TotalDebit,
    decimal Net,
    int TransactionCount);
