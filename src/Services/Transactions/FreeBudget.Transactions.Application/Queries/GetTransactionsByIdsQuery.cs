using FreeBudget.Transactions.Application.DTOs;
using FreeBudget.Transactions.Application.Interfaces;
using MediatR;

namespace FreeBudget.Transactions.Application.Queries;

public sealed record GetTransactionsByIdsQuery(IReadOnlyList<Guid> Ids)
    : IRequest<IReadOnlyList<TransactionListItem>>;

internal sealed class GetTransactionsByIdsHandler(ITransactionRepository repository)
    : IRequestHandler<GetTransactionsByIdsQuery, IReadOnlyList<TransactionListItem>>
{
    public async Task<IReadOnlyList<TransactionListItem>> Handle(GetTransactionsByIdsQuery request, CancellationToken cancellationToken)
    {
        var transactions = await repository.GetByIdsAsync(request.Ids, cancellationToken);
        return transactions
            .Select(t => new TransactionListItem(
                t.Id,
                t.BankAccountId,
                t.TransactionDate,
                t.Description,
                t.Amount.Amount,
                t.Amount.CurrencyCode,
                t.Direction.ToString(),
                t.Category,
                t.ExternalTransactionId,
                t.MatchedTransactionId))
            .ToList();
    }
}
