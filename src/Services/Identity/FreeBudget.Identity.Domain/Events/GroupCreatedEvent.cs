using FreeBudget.SharedKernel.Domain;

namespace FreeBudget.Identity.Domain.Events;

public sealed record GroupCreatedEvent(Guid GroupId, Guid CreatedByUserId) : DomainEvent;
