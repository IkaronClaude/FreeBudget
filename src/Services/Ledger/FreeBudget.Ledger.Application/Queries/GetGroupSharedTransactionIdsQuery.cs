using FreeBudget.Ledger.Application.Interfaces;
using MediatR;

namespace FreeBudget.Ledger.Application.Queries;

public sealed record GetGroupSharedTransactionIdsQuery(Guid GroupId) : IRequest<IReadOnlyList<Guid>>;

internal sealed class GetGroupSharedTransactionIdsHandler(ILedgerEntryRepository repository)
    : IRequestHandler<GetGroupSharedTransactionIdsQuery, IReadOnlyList<Guid>>
{
    public async Task<IReadOnlyList<Guid>> Handle(GetGroupSharedTransactionIdsQuery request, CancellationToken cancellationToken)
    {
        var entries = await repository.GetByGroupIdAsync(request.GroupId, cancellationToken);
        return entries
            .Where(e => e.TransactionId.HasValue)
            .Select(e => e.TransactionId!.Value)
            .Distinct()
            .ToList();
    }
}
