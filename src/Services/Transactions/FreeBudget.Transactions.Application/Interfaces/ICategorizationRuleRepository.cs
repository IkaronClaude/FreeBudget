using FreeBudget.Transactions.Domain.Entities;

namespace FreeBudget.Transactions.Application.Interfaces;

public interface ICategorizationRuleRepository
{
    Task<CategorizationRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategorizationRule>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(CategorizationRule rule, CancellationToken cancellationToken = default);
    Task UpdateAsync(CategorizationRule rule, CancellationToken cancellationToken = default);
    Task DeleteAsync(CategorizationRule rule, CancellationToken cancellationToken = default);
}
