namespace FreeBudget.Transactions.Application.DTOs;

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
