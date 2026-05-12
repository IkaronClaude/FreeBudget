using FreeBudget.SharedKernel.Results;
using FreeBudget.Transactions.Application.Interfaces;
using MediatR;

namespace FreeBudget.Transactions.Application.Commands;

public sealed record DeleteSharingRuleCommand(Guid RuleId) : IRequest<Result<bool>>;

internal sealed class DeleteSharingRuleHandler(ISharingRuleRepository repository)
    : IRequestHandler<DeleteSharingRuleCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteSharingRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await repository.GetByIdAsync(request.RuleId, cancellationToken);
        if (rule is null) return Result<bool>.Failure($"Sharing rule '{request.RuleId}' not found.");

        await repository.DeleteAsync(rule, cancellationToken);
        return Result<bool>.Success(true);
    }
}
