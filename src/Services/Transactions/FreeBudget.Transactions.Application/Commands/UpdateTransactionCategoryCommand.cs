using FreeBudget.SharedKernel.Results;
using FreeBudget.Transactions.Application.Interfaces;
using MediatR;

namespace FreeBudget.Transactions.Application.Commands;

public sealed record UpdateTransactionCategoryCommand(Guid TransactionId, string? Category)
    : IRequest<Result<bool>>;

internal sealed class UpdateTransactionCategoryHandler(
    ITransactionRepository repository)
    : IRequestHandler<UpdateTransactionCategoryCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(UpdateTransactionCategoryCommand request, CancellationToken cancellationToken)
    {
        var transaction = await repository.GetByIdAsync(request.TransactionId, cancellationToken);
        if (transaction is null)
            return Result<bool>.Failure($"Transaction '{request.TransactionId}' not found.");

        var trimmed = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim();
        transaction.UpdateCategory(trimmed);
        await repository.UpdateAsync(transaction, cancellationToken);

        return Result<bool>.Success(true);
    }
}
