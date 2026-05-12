using FreeBudget.Identity.Application.Interfaces;
using FreeBudget.SharedKernel.Results;
using MediatR;

namespace FreeBudget.Identity.Application.Commands;

public sealed record RenameBankAccountCommand(Guid BankAccountId, string Nickname)
    : IRequest<Result<bool>>;

internal sealed class RenameBankAccountHandler(IBankAccountRepository repository)
    : IRequestHandler<RenameBankAccountCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(RenameBankAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await repository.GetByIdAsync(request.BankAccountId, cancellationToken);
        if (account is null)
            return Result<bool>.Failure($"Bank account '{request.BankAccountId}' not found.");

        try
        {
            account.Rename(request.Nickname);
        }
        catch (ArgumentException ex)
        {
            return Result<bool>.Failure(ex.Message);
        }

        await repository.UpdateAsync(account, cancellationToken);
        return Result<bool>.Success(true);
    }
}
