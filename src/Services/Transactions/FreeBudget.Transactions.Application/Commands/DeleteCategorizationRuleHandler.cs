using FreeBudget.SharedKernel.Results;
using FreeBudget.Transactions.Application.Interfaces;
using MediatR;

namespace FreeBudget.Transactions.Application.Commands;

internal sealed class DeleteCategorizationRuleHandler(
    ICategorizationRuleRepository repository)
    : IRequestHandler<DeleteCategorizationRuleCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        DeleteCategorizationRuleCommand request,
        CancellationToken cancellationToken)
    {
        var rule = await repository.GetByIdAsync(request.RuleId, cancellationToken);
        if (rule is null)
            return Result<bool>.Failure("Rule not found.");

        await repository.DeleteAsync(rule, cancellationToken);
        return Result<bool>.Success(true);
    }
}
