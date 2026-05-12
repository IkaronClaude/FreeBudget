using FreeBudget.Transactions.Application.Interfaces;
using FreeBudget.Transactions.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FreeBudget.Transactions.Infrastructure.Persistence.Repositories;

internal sealed class SharingRuleRepository(TransactionsDbContext context) : ISharingRuleRepository
{
    public async Task<SharingRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.SharingRules.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<SharingRule>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await context.SharingRules
            .Where(r => r.CreatedByUserId == userId)
            .OrderByDescending(r => r.Priority)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(SharingRule rule, CancellationToken cancellationToken = default)
    {
        await context.SharingRules.AddAsync(rule, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(SharingRule rule, CancellationToken cancellationToken = default)
    {
        context.SharingRules.Update(rule);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(SharingRule rule, CancellationToken cancellationToken = default)
    {
        context.SharingRules.Remove(rule);
        await context.SaveChangesAsync(cancellationToken);
    }
}
