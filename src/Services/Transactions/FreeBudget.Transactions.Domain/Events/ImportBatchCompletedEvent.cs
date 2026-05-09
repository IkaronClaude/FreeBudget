using FreeBudget.SharedKernel.Domain;
using FreeBudget.Transactions.Domain.Enums;

namespace FreeBudget.Transactions.Domain.Events;

public sealed record ImportBatchCompletedEvent(
    Guid ImportBatchId,
    Guid BankAccountId,
    ImportStatus Status,
    int TransactionCount) : DomainEvent;
