using FreeBudget.SharedKernel.Domain;

namespace FreeBudget.Identity.Domain.Entities;

public sealed class BankAccountAccess : Entity<Guid>
{
    private BankAccountAccess() { }

    internal BankAccountAccess(Guid bankAccountId, Guid groupId)
    {
        Id = Guid.NewGuid();
        BankAccountId = bankAccountId;
        GroupId = groupId;
        GrantedAt = DateTime.UtcNow;
    }

    public Guid BankAccountId { get; private init; }
    public Guid GroupId { get; private init; }
    public DateTime GrantedAt { get; private init; }
}
