using FreeBudget.Transactions.Application.Interfaces;
using FreeBudget.Transactions.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FreeBudget.Transactions.Infrastructure.Persistence.Repositories;

internal sealed class TransactionRepository(TransactionsDbContext context) : ITransactionRepository
{
    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.Transactions.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Transaction>> GetByBankAccountIdAsync(Guid bankAccountId, CancellationToken cancellationToken = default)
        => await context.Transactions
            .Where(t => t.BankAccountId == bankAccountId)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync(cancellationToken);

    public async Task<bool> ExistsByExternalIdAsync(Guid bankAccountId, string externalTransactionId, CancellationToken cancellationToken = default)
        => await context.Transactions
            .AnyAsync(t => t.BankAccountId == bankAccountId && t.ExternalTransactionId == externalTransactionId, cancellationToken);

    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        await context.Transactions.AddAsync(transaction, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken = default)
    {
        await context.Transactions.AddRangeAsync(transactions, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
