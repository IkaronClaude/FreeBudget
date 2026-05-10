using FreeBudget.Transactions.Application.DTOs;
using FreeBudget.Transactions.Application.Interfaces;
using MediatR;

namespace FreeBudget.Transactions.Application.Queries;

public sealed record GetTransactionsByBankAccountQuery(
    Guid BankAccountId,
    DateTime? From = null,
    DateTime? To = null) : IRequest<IReadOnlyList<TransactionListItem>>;

internal sealed class GetTransactionsByBankAccountHandler(
    ITransactionRepository repository)
    : IRequestHandler<GetTransactionsByBankAccountQuery, IReadOnlyList<TransactionListItem>>
{
    public async Task<IReadOnlyList<TransactionListItem>> Handle(
        GetTransactionsByBankAccountQuery request,
        CancellationToken cancellationToken)
    {
        var transactions = request.From.HasValue && request.To.HasValue
            ? await repository.GetByBankAccountIdAndDateRangeAsync(
                request.BankAccountId, request.From.Value, request.To.Value, cancellationToken)
            : await repository.GetByBankAccountIdAsync(request.BankAccountId, cancellationToken);

        return transactions
            .OrderByDescending(t => t.TransactionDate)
            .Select(t => new TransactionListItem(
                t.Id,
                t.BankAccountId,
                t.TransactionDate,
                t.Description,
                t.Amount.Amount,
                t.Amount.CurrencyCode,
                t.Direction.ToString(),
                t.Category,
                t.ExternalTransactionId))
            .ToList();
    }
}
