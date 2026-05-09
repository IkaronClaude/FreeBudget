using FreeBudget.Transactions.Domain.Entities;

namespace FreeBudget.Transactions.Application.Interfaces;

public interface IImportBatchRepository
{
    Task<ImportBatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(ImportBatch importBatch, CancellationToken cancellationToken = default);
    Task UpdateAsync(ImportBatch importBatch, CancellationToken cancellationToken = default);
}
