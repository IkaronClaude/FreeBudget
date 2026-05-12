using FreeBudget.Identity.Application.Interfaces;
using FreeBudget.SharedKernel.Results;
using MediatR;

namespace FreeBudget.Identity.Application.Commands;

public sealed record GrantBankAccountAccessCommand(Guid BankAccountId, Guid GroupId) : IRequest<Result<bool>>;

internal sealed class GrantBankAccountAccessHandler(IBankAccountRepository repository)
    : IRequestHandler<GrantBankAccountAccessCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(GrantBankAccountAccessCommand request, CancellationToken cancellationToken)
    {
        var account = await repository.GetByIdAsync(request.BankAccountId, cancellationToken);
        if (account is null)
            return Result<bool>.Failure($"Bank account '{request.BankAccountId}' not found.");

        try
        {
            account.GrantAccessToGroup(request.GroupId);
        }
        catch (ArgumentException ex) { return Result<bool>.Failure(ex.Message); }
        catch (InvalidOperationException ex) { return Result<bool>.Failure(ex.Message); }

        await repository.UpdateAsync(account, cancellationToken);
        return Result<bool>.Success(true);
    }
}
