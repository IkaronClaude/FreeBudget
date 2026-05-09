using FreeBudget.Transactions.Application.Interfaces;
using FreeBudget.Transactions.Domain.Entities;
using MediatR;

namespace FreeBudget.Transactions.Application.Queries;

public sealed record GetCategorizationRulesQuery(Guid UserId)
    : IRequest<IReadOnlyList<CategorizationRule>>;

internal sealed class GetCategorizationRulesHandler(
    ICategorizationRuleRepository repository)
    : IRequestHandler<GetCategorizationRulesQuery, IReadOnlyList<CategorizationRule>>
{
    public Task<IReadOnlyList<CategorizationRule>> Handle(
        GetCategorizationRulesQuery request,
        CancellationToken cancellationToken)
    {
        return repository.GetByUserIdAsync(request.UserId, cancellationToken);
    }
}
