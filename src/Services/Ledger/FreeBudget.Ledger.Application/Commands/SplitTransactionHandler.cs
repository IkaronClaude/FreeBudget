using FreeBudget.Ledger.Application.Interfaces;
using FreeBudget.Ledger.Domain.Entities;
using FreeBudget.SharedKernel.Results;
using FreeBudget.SharedKernel.ValueObjects;
using MediatR;

namespace FreeBudget.Ledger.Application.Commands;

internal sealed class SplitTransactionHandler(ILedgerEntryRepository repository)
    : IRequestHandler<SplitTransactionCommand, Result<IReadOnlyList<Guid>>>
{
    public async Task<Result<IReadOnlyList<Guid>>> Handle(SplitTransactionCommand request, CancellationToken cancellationToken)
    {
        if (request.TransactionId == Guid.Empty)
            return Result<IReadOnlyList<Guid>>.Failure("TransactionId is required for a split.");

        if (request.Participants is null || request.Participants.Count == 0)
            return Result<IReadOnlyList<Guid>>.Failure("At least one participant is required.");

        if (request.Participants.Any(p => p.MemberId == request.PaidByMemberId))
            return Result<IReadOnlyList<Guid>>.Failure("Payer cannot be a participant in the split.");

        if (request.Participants.Any(p => p.Amount <= 0))
            return Result<IReadOnlyList<Guid>>.Failure("Participant amounts must be positive.");

        if (request.Participants.Select(p => p.MemberId).Distinct().Count() != request.Participants.Count)
            return Result<IReadOnlyList<Guid>>.Failure("Participants must be unique.");

        var existing = await repository.GetByTransactionIdAsync(request.TransactionId, cancellationToken);
        if (existing.Count > 0)
            return Result<IReadOnlyList<Guid>>.Failure("Transaction has already been split.");

        var entries = request.Participants
            .Select(p => LedgerEntry.CreateExpense(
                request.GroupId,
                request.PaidByMemberId,
                p.MemberId,
                new Money(p.Amount, request.CurrencyCode),
                request.Description,
                request.EntryDate,
                request.CreatedByUserId,
                request.TransactionId))
            .ToList();

        await repository.AddRangeAsync(entries, cancellationToken);
        return Result<IReadOnlyList<Guid>>.Success(entries.Select(e => e.Id).ToList());
    }
}
