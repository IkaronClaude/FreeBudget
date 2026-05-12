using FreeBudget.Transactions.Application.Interfaces;
using FreeBudget.Transactions.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FreeBudget.Transactions.Infrastructure.Persistence.Repositories;

internal sealed class ImportLayoutRepository(TransactionsDbContext context) : IImportLayoutRepository
{
    public async Task<ImportLayoutDefinition?> GetByBankAccountIdAsync(Guid bankAccountId, CancellationToken cancellationToken = default)
        => await context.ImportLayouts.FirstOrDefaultAsync(l => l.BankAccountId == bankAccountId, cancellationToken);

    public async Task AddAsync(ImportLayoutDefinition layout, CancellationToken cancellationToken = default)
    {
        await context.ImportLayouts.AddAsync(layout, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ImportLayoutDefinition layout, CancellationToken cancellationToken = default)
    {
        context.ImportLayouts.Update(layout);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(ImportLayoutDefinition layout, CancellationToken cancellationToken = default)
    {
        context.ImportLayouts.Remove(layout);
        await context.SaveChangesAsync(cancellationToken);
    }
}
