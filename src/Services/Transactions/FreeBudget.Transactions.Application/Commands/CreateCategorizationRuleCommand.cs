using FreeBudget.SharedKernel.Results;
using FreeBudget.Transactions.Domain.Enums;
using MediatR;

namespace FreeBudget.Transactions.Application.Commands;

public sealed record CreateCategorizationRuleCommand(
    Guid CreatedByUserId,
    string Pattern,
    RuleMatchType MatchType,
    string Category,
    int Priority = 0) : IRequest<Result<Guid>>;
