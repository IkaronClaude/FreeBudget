using FreeBudget.SharedKernel.Results;
using FreeBudget.Transactions.Application.Interfaces;
using MediatR;

namespace FreeBudget.Transactions.Application.Commands;

internal sealed class UpdateCategorizationRuleHandler(
    ICategorizationRuleRepository repository)
    : IRequestHandler<UpdateCategorizationRuleCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        UpdateCategorizationRuleCommand request,
        CancellationToken cancellationToken)
    {
        var rule = await repository.GetByIdAsync(request.RuleId, cancellationToken);
        if (rule is null)
            return Result<bool>.Failure("Rule not found.");

        rule.Update(request.Pattern, request.MatchType, request.Category, request.Priority);
        await repository.UpdateAsync(rule, cancellationToken);
        return Result<bool>.Success(true);
    }
}
