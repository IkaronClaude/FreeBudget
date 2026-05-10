using FreeBudget.Transactions.Application.DTOs;
using FreeBudget.Transactions.Application.Interfaces;
using FreeBudget.SharedKernel.ValueObjects;
using FreeBudget.Transactions.Domain.ValueObjects;
using MediatR;

namespace FreeBudget.Transactions.Application.Queries;

public sealed record GetCategoryBreakdownQuery(
    Guid BankAccountId,
    DateTime From,
    DateTime To) : IRequest<IReadOnlyList<CategoryBreakdownItem>>;

internal sealed class GetCategoryBreakdownHandler(
    ITransactionRepository repository)
    : IRequestHandler<GetCategoryBreakdownQuery, IReadOnlyList<CategoryBreakdownItem>>
{
    public async Task<IReadOnlyList<CategoryBreakdownItem>> Handle(
        GetCategoryBreakdownQuery request,
        CancellationToken cancellationToken)
    {
        var transactions = await repository.GetByBankAccountIdAndDateRangeAsync(
            request.BankAccountId, request.From, request.To, cancellationToken);

        return transactions
            .GroupBy(t => t.Category ?? "Uncategorized")
            .Select(g => new CategoryBreakdownItem(
                Category: g.Key,
                TotalCredit: g.Where(t => t.Direction == TransactionDirection.Credit).Sum(t => t.Amount.Amount),
                TotalDebit: g.Where(t => t.Direction == TransactionDirection.Debit).Sum(t => t.Amount.Amount),
                Net: g.Where(t => t.Direction == TransactionDirection.Credit).Sum(t => t.Amount.Amount)
                   - g.Where(t => t.Direction == TransactionDirection.Debit).Sum(t => t.Amount.Amount),
                TransactionCount: g.Count()))
            .OrderByDescending(c => c.TotalDebit)
            .ToList();
    }
}
