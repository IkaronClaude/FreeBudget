using FreeBudget.SharedKernel.Results;
using FreeBudget.Transactions.Domain.Enums;
using MediatR;

namespace FreeBudget.Transactions.Application.Commands;

public sealed record UpdateCategorizationRuleCommand(
    Guid RuleId,
    string Pattern,
    RuleMatchType MatchType,
    string Category,
    int Priority) : IRequest<Result<bool>>;
