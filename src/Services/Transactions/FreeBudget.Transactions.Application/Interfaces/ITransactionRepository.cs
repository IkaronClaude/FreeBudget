using FreeBudget.Transactions.Domain.Entities;

namespace FreeBudget.Transactions.Application.Interfaces;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Transaction>> GetByBankAccountIdAsync(Guid bankAccountId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Transaction>> GetByBankAccountIdAndDateRangeAsync(Guid bankAccountId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<bool> ExistsByExternalIdAsync(Guid bankAccountId, string externalTransactionId, CancellationToken cancellationToken = default);
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken = default);
}
