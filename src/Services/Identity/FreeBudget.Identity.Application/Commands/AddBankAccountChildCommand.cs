using FreeBudget.Identity.Application.DTOs;
using FreeBudget.Identity.Application.Interfaces;
using FreeBudget.Identity.Domain.Entities;
using FreeBudget.SharedKernel.Results;
using MediatR;

namespace FreeBudget.Identity.Application.Commands;

public sealed record AddBankAccountChildCommand(
    Guid ParentBankAccountId,
    string CurrencyCode) : IRequest<Result<BankAccountDto>>;

internal sealed class AddBankAccountChildHandler(IBankAccountRepository repository)
    : IRequestHandler<AddBankAccountChildCommand, Result<BankAccountDto>>
{
    public async Task<Result<BankAccountDto>> Handle(AddBankAccountChildCommand request, CancellationToken cancellationToken)
    {
        var parent = await repository.GetByIdAsync(request.ParentBankAccountId, cancellationToken);
        if (parent is null)
            return Result<BankAccountDto>.Failure($"Bank account '{request.ParentBankAccountId}' not found.");
        if (parent.IsChild)
            return Result<BankAccountDto>.Failure("Cannot attach a currency to a child account; pick the parent.");

        var normalised = request.CurrencyCode?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(normalised))
            return Result<BankAccountDto>.Failure("Currency code is required.");

        var existing = await repository.GetChildrenAsync(parent.Id, cancellationToken);
        if (existing.Any(c => string.Equals(c.CurrencyCode, normalised, StringComparison.OrdinalIgnoreCase)))
            return Result<BankAccountDto>.Failure($"Currency '{normalised}' is already attached to this account.");

        BankAccount child;
        try
        {
            child = BankAccount.CreateChild(parent, normalised);
        }
        catch (ArgumentException ex) { return Result<BankAccountDto>.Failure(ex.Message); }
        catch (InvalidOperationException ex) { return Result<BankAccountDto>.Failure(ex.Message); }

        await repository.AddAsync(child, cancellationToken);

        return Result<BankAccountDto>.Success(new BankAccountDto(
            child.Id,
            child.OwnerUserId,
            child.BankType.Name,
            child.Nickname,
            child.ExternalAccountId,
            child.HasApiCredentials,
            child.ParentBankAccountId,
            child.CurrencyCode,
            child.AccessGrants.Select(g => g.GroupId).ToList()));
    }
}
