using FreeBudget.SharedKernel.Results;
using FreeBudget.Transactions.Application.Interfaces;
using MediatR;

namespace FreeBudget.Transactions.Application.Commands;

public sealed record DeleteImportLayoutCommand(Guid BankAccountId) : IRequest<Result<bool>>;

internal sealed class DeleteImportLayoutHandler(IImportLayoutRepository repository)
    : IRequestHandler<DeleteImportLayoutCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteImportLayoutCommand request, CancellationToken cancellationToken)
    {
        var layout = await repository.GetByBankAccountIdAsync(request.BankAccountId, cancellationToken);
        if (layout is null)
            return Result<bool>.Failure($"No import layout for bank account '{request.BankAccountId}'.");

        await repository.DeleteAsync(layout, cancellationToken);
        return Result<bool>.Success(true);
    }
}
