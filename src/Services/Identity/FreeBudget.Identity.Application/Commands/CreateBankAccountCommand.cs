using FreeBudget.Identity.Application.DTOs;
using FreeBudget.Identity.Application.Interfaces;
using FreeBudget.Identity.Domain.Entities;
using FreeBudget.Identity.Domain.ValueObjects;
using FreeBudget.SharedKernel.Results;
using MediatR;

namespace FreeBudget.Identity.Application.Commands;

public sealed record CreateBankAccountCommand(
    Guid OwnerUserId,
    string BankType,
    string Nickname,
    string? CurrencyCode = null) : IRequest<Result<BankAccountDto>>;

internal sealed class CreateBankAccountHandler(IBankAccountRepository repository)
    : IRequestHandler<CreateBankAccountCommand, Result<BankAccountDto>>
{
    public async Task<Result<BankAccountDto>> Handle(CreateBankAccountCommand request, CancellationToken cancellationToken)
    {
        BankType bankType;
        try
        {
            bankType = BankType.From(request.BankType);
        }
        catch (ArgumentException ex)
        {
            return Result<BankAccountDto>.Failure(ex.Message);
        }

        var account = BankAccount.Create(request.OwnerUserId, bankType, request.Nickname, request.CurrencyCode);
        await repository.AddAsync(account, cancellationToken);

        return Result<BankAccountDto>.Success(new BankAccountDto(
            account.Id,
            account.OwnerUserId,
            account.BankType.Name,
            account.Nickname,
            account.ExternalAccountId,
            account.HasApiCredentials,
            account.ParentBankAccountId,
            account.CurrencyCode,
            account.AccessGrants.Select(g => g.GroupId).ToList()));
    }
}
