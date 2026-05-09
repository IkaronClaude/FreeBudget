using FreeBudget.Ledger.Application.Interfaces;
using FreeBudget.Ledger.Domain.Entities;
using MediatR;

namespace FreeBudget.Ledger.Application.Queries;

public sealed record GetGroupLedgerEntriesQuery(Guid GroupId)
    : IRequest<IReadOnlyList<LedgerEntry>>;

internal sealed class GetGroupLedgerEntriesHandler(ILedgerEntryRepository repository)
    : IRequestHandler<GetGroupLedgerEntriesQuery, IReadOnlyList<LedgerEntry>>
{
    public Task<IReadOnlyList<LedgerEntry>> Handle(
        GetGroupLedgerEntriesQuery request,
        CancellationToken cancellationToken)
    {
        return repository.GetByGroupIdAsync(request.GroupId, cancellationToken);
    }
}
