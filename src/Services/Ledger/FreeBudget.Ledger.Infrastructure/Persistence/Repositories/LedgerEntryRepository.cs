using FreeBudget.Ledger.Application.Interfaces;
using FreeBudget.Ledger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FreeBudget.Ledger.Infrastructure.Persistence.Repositories;

internal sealed class LedgerEntryRepository(LedgerDbContext context) : ILedgerEntryRepository
{
    public async Task<LedgerEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.LedgerEntries.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<IReadOnlyList<LedgerEntry>> GetByGroupIdAsync(Guid groupId, CancellationToken cancellationToken = default)
        => await context.LedgerEntries
            .Where(e => e.GroupId == groupId)
            .OrderByDescending(e => e.EntryDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<LedgerEntry>> GetByTransactionIdAsync(Guid transactionId, CancellationToken cancellationToken = default)
        => await context.LedgerEntries
            .Where(e => e.TransactionId == transactionId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(LedgerEntry entry, CancellationToken cancellationToken = default)
    {
        await context.LedgerEntries.AddAsync(entry, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<LedgerEntry> entries, CancellationToken cancellationToken = default)
    {
        await context.LedgerEntries.AddRangeAsync(entries, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(LedgerEntry entry, CancellationToken cancellationToken = default)
    {
        context.LedgerEntries.Remove(entry);
        await context.SaveChangesAsync(cancellationToken);
    }
}
