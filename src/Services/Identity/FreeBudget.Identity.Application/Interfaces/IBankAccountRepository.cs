using FreeBudget.Identity.Domain.Entities;

namespace FreeBudget.Identity.Application.Interfaces;

public interface IBankAccountRepository
{
    Task<BankAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BankAccount>> GetByOwnerUserIdAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BankAccount>> GetByGroupAccessAsync(Guid groupId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BankAccount>> GetChildrenAsync(Guid parentId, CancellationToken cancellationToken = default);
    Task AddAsync(BankAccount bankAccount, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<BankAccount> bankAccounts, CancellationToken cancellationToken = default);
    Task UpdateAsync(BankAccount bankAccount, CancellationToken cancellationToken = default);
    Task DeleteAsync(BankAccount bankAccount, CancellationToken cancellationToken = default);
}
