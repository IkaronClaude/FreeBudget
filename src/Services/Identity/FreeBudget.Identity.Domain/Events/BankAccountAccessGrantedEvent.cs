using FreeBudget.SharedKernel.Domain;

namespace FreeBudget.Identity.Domain.Events;

public sealed record BankAccountAccessGrantedEvent(Guid BankAccountId, Guid GroupId) : DomainEvent;
