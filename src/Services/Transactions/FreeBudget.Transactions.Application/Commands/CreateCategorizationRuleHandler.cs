using FreeBudget.SharedKernel.Results;
using FreeBudget.Transactions.Application.Interfaces;
using FreeBudget.Transactions.Domain.Entities;
using MediatR;

namespace FreeBudget.Transactions.Application.Commands;

internal sealed class CreateCategorizationRuleHandler(
    ICategorizationRuleRepository repository)
    : IRequestHandler<CreateCategorizationRuleCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateCategorizationRuleCommand request,
        CancellationToken cancellationToken)
    {
        var rule = CategorizationRule.Create(
            request.CreatedByUserId,
            request.Pattern,
            request.MatchType,
            request.Category,
            request.Priority);

        await repository.AddAsync(rule, cancellationToken);
        return Result<Guid>.Success(rule.Id);
    }
}
