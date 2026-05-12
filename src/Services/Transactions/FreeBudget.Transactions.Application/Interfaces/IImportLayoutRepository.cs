using FreeBudget.Transactions.Domain.Entities;

namespace FreeBudget.Transactions.Application.Interfaces;

public interface IImportLayoutRepository
{
    Task<ImportLayoutDefinition?> GetByBankAccountIdAsync(Guid bankAccountId, CancellationToken cancellationToken = default);
    Task AddAsync(ImportLayoutDefinition layout, CancellationToken cancellationToken = default);
    Task UpdateAsync(ImportLayoutDefinition layout, CancellationToken cancellationToken = default);
    Task DeleteAsync(ImportLayoutDefinition layout, CancellationToken cancellationToken = default);
}
