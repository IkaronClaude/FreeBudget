using FreeBudget.Ledger.Domain.Entities;

namespace FreeBudget.Ledger.Application.Interfaces;

public interface ILedgerEntryRepository
{
    Task<LedgerEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LedgerEntry>> GetByGroupIdAsync(Guid groupId, CancellationToken cancellationToken = default);
    Task AddAsync(LedgerEntry entry, CancellationToken cancellationToken = default);
    Task DeleteAsync(LedgerEntry entry, CancellationToken cancellationToken = default);
}
