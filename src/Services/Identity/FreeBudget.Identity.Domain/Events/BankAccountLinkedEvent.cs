using FreeBudget.SharedKernel.Domain;

namespace FreeBudget.Identity.Domain.Events;

public sealed record BankAccountLinkedEvent(Guid BankAccountId, Guid OwnerUserId, string BankType) : DomainEvent;
