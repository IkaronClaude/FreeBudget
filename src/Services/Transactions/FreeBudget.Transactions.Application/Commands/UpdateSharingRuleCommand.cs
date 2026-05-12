using FreeBudget.SharedKernel.Results;
using FreeBudget.Transactions.Application.Interfaces;
using FreeBudget.Transactions.Domain.Enums;
using MediatR;

namespace FreeBudget.Transactions.Application.Commands;

public sealed record UpdateSharingRuleCommand(
    Guid RuleId,
    string Pattern,
    RuleMatchType MatchType,
    int Priority,
    Guid GroupId,
    Guid PaidByMemberId,
    IReadOnlyList<Guid> ParticipantMemberIds) : IRequest<Result<bool>>;

internal sealed class UpdateSharingRuleHandler(ISharingRuleRepository repository)
    : IRequestHandler<UpdateSharingRuleCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(UpdateSharingRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await repository.GetByIdAsync(request.RuleId, cancellationToken);
        if (rule is null) return Result<bool>.Failure($"Sharing rule '{request.RuleId}' not found.");

        try
        {
            rule.Update(request.Pattern, request.MatchType, request.GroupId,
                request.PaidByMemberId, request.ParticipantMemberIds, request.Priority);
        }
        catch (ArgumentException ex)
        {
            return Result<bool>.Failure(ex.Message);
        }

        await repository.UpdateAsync(rule, cancellationToken);
        return Result<bool>.Success(true);
    }
}
