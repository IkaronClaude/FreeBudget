using FreeBudget.SharedKernel.Results;
using MediatR;

namespace FreeBudget.Transactions.Application.Commands;

public sealed record DeleteCategorizationRuleCommand(Guid RuleId) : IRequest<Result<bool>>;
