using FreeBudget.SharedKernel.Results;
using MediatR;

namespace FreeBudget.Ledger.Application.Commands;

public sealed record CreateSettlementCommand(
    Guid GroupId,
    Guid PaidByMemberId,
    Guid OwedByMemberId,
    decimal Amount,
    string CurrencyCode,
    string Description,
    DateTime EntryDate,
    Guid CreatedByUserId,
    Guid? TransactionId = null) : IRequest<Result<Guid>>;
