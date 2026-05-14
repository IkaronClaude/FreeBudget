using FreeBudget.Identity.Application.DTOs;
using FreeBudget.Identity.Application.Interfaces;
using MediatR;

namespace FreeBudget.Identity.Application.Queries;

public sealed record GetUserBankAccountsQuery(Guid UserId) : IRequest<IReadOnlyList<BankAccountDto>>;

internal sealed class GetUserBankAccountsHandler(IBankAccountRepository repository)
    : IRequestHandler<GetUserBankAccountsQuery, IReadOnlyList<BankAccountDto>>
{
    public async Task<IReadOnlyList<BankAccountDto>> Handle(GetUserBankAccountsQuery request, CancellationToken cancellationToken)
    {
        var accounts = await repository.GetByOwnerUserIdAsync(request.UserId, cancellationToken);
        var byId = accounts.ToDictionary(a => a.Id);
        return accounts
            .Select(a =>
            {
                var grantSource = a.ParentBankAccountId is not null && byId.TryGetValue(a.ParentBankAccountId.Value, out var parent)
                    ? parent
                    : a;
                return new BankAccountDto(
                    a.Id,
                    a.OwnerUserId,
                    a.BankType.Name,
                    a.Nickname,
                    a.ExternalAccountId,
                    a.HasApiCredentials,
                    a.ParentBankAccountId,
                    a.CurrencyCode,
                    grantSource.AccessGrants.Select(g => g.GroupId).ToList());
            })
            .ToList();
    }
}
