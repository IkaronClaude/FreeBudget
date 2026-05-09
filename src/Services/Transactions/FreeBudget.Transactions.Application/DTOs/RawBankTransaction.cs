namespace FreeBudget.Transactions.Application.DTOs;

public sealed record RawBankTransaction(
    string? ExternalTransactionId,
    DateTime TransactionDate,
    string Description,
    decimal Amount,
    string CurrencyCode,
    string Direction,
    decimal? RunningBalance);
