using FreeBudget.Transactions.Application.DTOs;
using FreeBudget.Transactions.Application.Interfaces;
using MediatR;

namespace FreeBudget.Transactions.Application.Queries;

public sealed record GetSharingRulesQuery(Guid UserId) : IRequest<IReadOnlyList<SharingRuleDto>>;

internal sealed class GetSharingRulesHandler(ISharingRuleRepository repository)
    : IRequestHandler<GetSharingRulesQuery, IReadOnlyList<SharingRuleDto>>
{
    public async Task<IReadOnlyList<SharingRuleDto>> Handle(GetSharingRulesQuery request, CancellationToken cancellationToken)
    {
        var rules = await repository.GetByUserIdAsync(request.UserId, cancellationToken);
        return rules.Select(r => new SharingRuleDto(
            r.Id, r.Pattern, r.RuleMatchType.ToString(), r.Priority,
            r.GroupId, r.PaidByMemberId, r.ParticipantMemberIds)).ToList();
    }
}
