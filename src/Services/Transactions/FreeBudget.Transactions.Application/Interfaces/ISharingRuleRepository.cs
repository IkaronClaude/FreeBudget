using FreeBudget.Transactions.Domain.Entities;

namespace FreeBudget.Transactions.Application.Interfaces;

public interface ISharingRuleRepository
{
    Task<SharingRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SharingRule>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(SharingRule rule, CancellationToken cancellationToken = default);
    Task UpdateAsync(SharingRule rule, CancellationToken cancellationToken = default);
    Task DeleteAsync(SharingRule rule, CancellationToken cancellationToken = default);
}
