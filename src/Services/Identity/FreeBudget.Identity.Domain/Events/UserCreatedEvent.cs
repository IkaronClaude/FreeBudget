using FreeBudget.SharedKernel.Domain;

namespace FreeBudget.Identity.Domain.Events;

public sealed record UserCreatedEvent(Guid UserId, string Email) : DomainEvent;
