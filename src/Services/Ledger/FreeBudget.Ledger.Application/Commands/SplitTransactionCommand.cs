using FreeBudget.SharedKernel.Results;
using MediatR;

namespace FreeBudget.Ledger.Application.Commands;

public sealed record SplitTransactionCommand(
    Guid GroupId,
    Guid PaidByUserId,
    Guid TransactionId,
    string CurrencyCode,
    string Description,
    DateTime EntryDate,
    Guid CreatedByUserId,
    IReadOnlyList<SplitParticipant> Participants) : IRequest<Result<IReadOnlyList<Guid>>>;

public sealed record SplitParticipant(Guid UserId, decimal Amount);
