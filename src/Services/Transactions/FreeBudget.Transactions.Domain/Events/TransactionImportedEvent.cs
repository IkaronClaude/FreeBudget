using FreeBudget.SharedKernel.Domain;

namespace FreeBudget.Transactions.Domain.Events;

public sealed record TransactionImportedEvent(
    Guid TransactionId,
    Guid BankAccountId,
    decimal Amount,
    string CurrencyCode) : DomainEvent;
