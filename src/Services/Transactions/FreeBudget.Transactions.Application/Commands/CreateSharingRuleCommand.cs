using FreeBudget.SharedKernel.Results;
using FreeBudget.Transactions.Application.Interfaces;
using FreeBudget.Transactions.Domain.Entities;
using FreeBudget.Transactions.Domain.Enums;
using MediatR;

namespace FreeBudget.Transactions.Application.Commands;

public sealed record CreateSharingRuleCommand(
    Guid UserId,
    string Pattern,
    RuleMatchType MatchType,
    LedgerEntryKind EntryType,
    int Priority,
    Guid GroupId,
    Guid PaidByMemberId,
    IReadOnlyList<Guid> ParticipantMemberIds) : IRequest<Result<Guid>>;

internal sealed class CreateSharingRuleHandler(ISharingRuleRepository repository)
    : IRequestHandler<CreateSharingRuleCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateSharingRuleCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var rule = SharingRule.Create(
                request.UserId, request.Pattern, request.MatchType, request.EntryType,
                request.GroupId, request.PaidByMemberId, request.ParticipantMemberIds, request.Priority);
            await repository.AddAsync(rule, cancellationToken);
            return Result<Guid>.Success(rule.Id);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message);
        }
    }
}
