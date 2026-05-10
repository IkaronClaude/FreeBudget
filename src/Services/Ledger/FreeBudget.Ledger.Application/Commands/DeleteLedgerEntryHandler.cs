using FreeBudget.Ledger.Application.Interfaces;
using FreeBudget.SharedKernel.Results;
using MediatR;

namespace FreeBudget.Ledger.Application.Commands;

internal sealed class DeleteLedgerEntryHandler(ILedgerEntryRepository repository)
    : IRequestHandler<DeleteLedgerEntryCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteLedgerEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = await repository.GetByIdAsync(request.EntryId, cancellationToken);
        if (entry is null)
            return Result<bool>.Failure("Ledger entry not found.");

        await repository.DeleteAsync(entry, cancellationToken);
        return Result<bool>.Success(true);
    }
}
