using FreeBudget.SharedKernel.Results;
using FreeBudget.Transactions.Application.Interfaces;
using MediatR;

namespace FreeBudget.Transactions.Application.Commands;

public sealed record BulkUpdateTransactionCategoryResult(int Updated, int NotFound);

public sealed record BulkUpdateTransactionCategoryCommand(IReadOnlyList<Guid> TransactionIds, string? Category)
    : IRequest<Result<BulkUpdateTransactionCategoryResult>>;

internal sealed class BulkUpdateTransactionCategoryHandler(
    ITransactionRepository repository)
    : IRequestHandler<BulkUpdateTransactionCategoryCommand, Result<BulkUpdateTransactionCategoryResult>>
{
    public async Task<Result<BulkUpdateTransactionCategoryResult>> Handle(
        BulkUpdateTransactionCategoryCommand request,
        CancellationToken cancellationToken)
    {
        if (request.TransactionIds.Count == 0)
            return Result<BulkUpdateTransactionCategoryResult>.Success(new(0, 0));

        var trimmed = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim();
        var transactions = await repository.GetByIdsAsync(request.TransactionIds, cancellationToken);

        foreach (var transaction in transactions)
        {
            transaction.UpdateCategory(trimmed);
        }

        if (transactions.Count > 0)
            await repository.UpdateRangeAsync(transactions, cancellationToken);

        var notFound = request.TransactionIds.Count - transactions.Count;
        return Result<BulkUpdateTransactionCategoryResult>.Success(
            new BulkUpdateTransactionCategoryResult(transactions.Count, notFound));
    }
}
