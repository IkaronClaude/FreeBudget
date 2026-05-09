using FreeBudget.Ledger.Domain.Enums;
using FreeBudget.SharedKernel.Domain;

namespace FreeBudget.Ledger.Domain.Events;

public sealed record LedgerEntryCreatedEvent(
    Guid EntryId,
    Guid GroupId,
    Guid PaidByUserId,
    Guid OwedByUserId,
    decimal Amount,
    string CurrencyCode,
    LedgerEntryType EntryType) : DomainEvent;
