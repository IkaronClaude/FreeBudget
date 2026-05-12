using FreeBudget.Ledger.Domain.Enums;
using FreeBudget.SharedKernel.Domain;

namespace FreeBudget.Ledger.Domain.Events;

public sealed record LedgerEntryCreatedEvent(
    Guid EntryId,
    Guid GroupId,
    Guid PaidByMemberId,
    Guid OwedByMemberId,
    decimal Amount,
    string CurrencyCode,
    LedgerEntryType EntryType) : DomainEvent;
