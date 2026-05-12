using FreeBudget.Identity.Application.Interfaces;
using FreeBudget.SharedKernel.Results;
using MediatR;

namespace FreeBudget.Identity.Application.Commands;

public sealed record DeleteBankAccountCommand(Guid BankAccountId) : IRequest<Result<bool>>;

internal sealed class DeleteBankAccountHandler(IBankAccountRepository repository)
    : IRequestHandler<DeleteBankAccountCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteBankAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await repository.GetByIdAsync(request.BankAccountId, cancellationToken);
        if (account is null)
            return Result<bool>.Failure($"Bank account '{request.BankAccountId}' not found.");

        await repository.DeleteAsync(account, cancellationToken);
        return Result<bool>.Success(true);
    }
}
