using FreeBudget.Ledger.Application.Interfaces;
using FreeBudget.Ledger.Domain.Entities;
using FreeBudget.SharedKernel.Results;
using FreeBudget.SharedKernel.ValueObjects;
using MediatR;

namespace FreeBudget.Ledger.Application.Commands;

internal sealed class CreateExpenseHandler(ILedgerEntryRepository repository)
    : IRequestHandler<CreateExpenseCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateExpenseCommand request, CancellationToken cancellationToken)
    {
        var amount = new Money(request.Amount, request.CurrencyCode);
        var entry = LedgerEntry.CreateExpense(
            request.GroupId,
            request.PaidByMemberId,
            request.OwedByMemberId,
            amount,
            request.Description,
            request.EntryDate,
            request.CreatedByUserId,
            request.TransactionId);

        await repository.AddAsync(entry, cancellationToken);
        return Result<Guid>.Success(entry.Id);
    }
}
