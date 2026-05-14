using FreeBudget.Identity.Application.DTOs;
using FreeBudget.Identity.Application.Interfaces;
using MediatR;

namespace FreeBudget.Identity.Application.Queries;

public sealed record GetGroupBankAccountsQuery(Guid GroupId) : IRequest<IReadOnlyList<BankAccountDto>>;

internal sealed class GetGroupBankAccountsHandler(IBankAccountRepository repository)
    : IRequestHandler<GetGroupBankAccountsQuery, IReadOnlyList<BankAccountDto>>
{
    public async Task<IReadOnlyList<BankAccountDto>> Handle(GetGroupBankAccountsQuery request, CancellationToken cancellationToken)
    {
        var accounts = await repository.GetByGroupAccessAsync(request.GroupId, cancellationToken);
        return accounts
            .Select(a => new BankAccountDto(
                a.Id,
                a.OwnerUserId,
                a.BankType.Name,
                a.Nickname,
                a.ExternalAccountId,
                a.HasApiCredentials,
                a.ParentBankAccountId,
                a.CurrencyCode,
                a.AccessGrants.Select(g => g.GroupId).ToList()))
            .ToList();
    }
}
