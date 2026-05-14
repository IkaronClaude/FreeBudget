using FreeBudget.Identity.Application.DTOs;
using FreeBudget.Identity.Application.Interfaces;
using FreeBudget.Identity.Domain.Entities;
using FreeBudget.Identity.Domain.ValueObjects;
using FreeBudget.SharedKernel.Results;
using MediatR;

namespace FreeBudget.Identity.Application.Commands;

public sealed record CreateParentBankAccountCommand(
    Guid OwnerUserId,
    string BankType,
    string Nickname,
    IReadOnlyList<string> CurrencyCodes) : IRequest<Result<BankAccountDto>>;

internal sealed class CreateParentBankAccountHandler(IBankAccountRepository repository)
    : IRequestHandler<CreateParentBankAccountCommand, Result<BankAccountDto>>
{
    public async Task<Result<BankAccountDto>> Handle(CreateParentBankAccountCommand request, CancellationToken cancellationToken)
    {
        if (request.CurrencyCodes is null || request.CurrencyCodes.Count == 0)
            return Result<BankAccountDto>.Failure("At least one currency code is required for a parent bank account.");

        BankType bankType;
        try
        {
            bankType = BankType.From(request.BankType);
        }
        catch (ArgumentException ex)
        {
            return Result<BankAccountDto>.Failure(ex.Message);
        }

        BankAccount parent;
        List<BankAccount> children;
        try
        {
            parent = BankAccount.CreateParent(request.OwnerUserId, bankType, request.Nickname);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            children = new List<BankAccount>(request.CurrencyCodes.Count);
            foreach (var raw in request.CurrencyCodes)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var normalised = raw.Trim().ToUpperInvariant();
                if (!seen.Add(normalised)) continue;
                children.Add(BankAccount.CreateChild(parent, normalised));
            }
            if (children.Count == 0)
                return Result<BankAccountDto>.Failure("At least one currency code is required for a parent bank account.");
        }
        catch (ArgumentException ex) { return Result<BankAccountDto>.Failure(ex.Message); }
        catch (InvalidOperationException ex) { return Result<BankAccountDto>.Failure(ex.Message); }

        var all = new List<BankAccount> { parent };
        all.AddRange(children);
        await repository.AddRangeAsync(all, cancellationToken);

        return Result<BankAccountDto>.Success(new BankAccountDto(
            parent.Id,
            parent.OwnerUserId,
            parent.BankType.Name,
            parent.Nickname,
            parent.ExternalAccountId,
            parent.HasApiCredentials,
            parent.ParentBankAccountId,
            parent.CurrencyCode,
            parent.AccessGrants.Select(g => g.GroupId).ToList()));
    }
}
