using FreeBudget.Transactions.Application.Interfaces;
using FreeBudget.Transactions.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FreeBudget.Transactions.Infrastructure.Persistence.Repositories;

internal sealed class CategorizationRuleRepository(TransactionsDbContext context) : ICategorizationRuleRepository
{
    public async Task<CategorizationRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.CategorizationRules.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CategorizationRule>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await context.CategorizationRules
            .Where(r => r.CreatedByUserId == userId)
            .OrderByDescending(r => r.Priority)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(CategorizationRule rule, CancellationToken cancellationToken = default)
    {
        await context.CategorizationRules.AddAsync(rule, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(CategorizationRule rule, CancellationToken cancellationToken = default)
    {
        context.CategorizationRules.Update(rule);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(CategorizationRule rule, CancellationToken cancellationToken = default)
    {
        context.CategorizationRules.Remove(rule);
        await context.SaveChangesAsync(cancellationToken);
    }
}
