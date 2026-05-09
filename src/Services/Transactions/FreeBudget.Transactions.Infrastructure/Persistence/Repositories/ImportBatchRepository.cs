using FreeBudget.Transactions.Application.Interfaces;
using FreeBudget.Transactions.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FreeBudget.Transactions.Infrastructure.Persistence.Repositories;

internal sealed class ImportBatchRepository(TransactionsDbContext context) : IImportBatchRepository
{
    public async Task<ImportBatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.ImportBatches.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task AddAsync(ImportBatch importBatch, CancellationToken cancellationToken = default)
    {
        await context.ImportBatches.AddAsync(importBatch, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ImportBatch importBatch, CancellationToken cancellationToken = default)
    {
        context.ImportBatches.Update(importBatch);
        await context.SaveChangesAsync(cancellationToken);
    }
}
