using FreeBudget.SharedKernel.Results;
using MediatR;

namespace FreeBudget.Ledger.Application.Commands;

public sealed record CreateExpenseCommand(
    Guid GroupId,
    Guid PaidByUserId,
    Guid OwedByUserId,
    decimal Amount,
    string CurrencyCode,
    string Description,
    DateTime EntryDate,
    Guid CreatedByUserId,
    Guid? TransactionId = null) : IRequest<Result<Guid>>;
