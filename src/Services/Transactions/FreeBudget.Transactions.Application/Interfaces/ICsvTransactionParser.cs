using FreeBudget.Transactions.Application.DTOs;

namespace FreeBudget.Transactions.Application.Interfaces;

public interface ICsvTransactionParser
{
    Task<IReadOnlyList<RawBankTransaction>> ParseAsync(
        Stream csvStream,
        ImportLayout layout,
        CancellationToken cancellationToken = default);
}
