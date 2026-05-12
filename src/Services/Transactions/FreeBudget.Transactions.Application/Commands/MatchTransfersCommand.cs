using FreeBudget.SharedKernel.Results;
using FreeBudget.Transactions.Application.Interfaces;
using FreeBudget.Transactions.Domain.ValueObjects;
using MediatR;

namespace FreeBudget.Transactions.Application.Commands;

public sealed record MatchTransfersCommand(
    IReadOnlyList<Guid> BankAccountIds,
    IReadOnlyList<Guid>? RestrictToTransactionIds = null,
    int DateToleranceDays = 1) : IRequest<Result<MatchTransfersResult>>;

public sealed record MatchTransfersResult(int Examined, int Matched, int AmbiguousSkipped);

internal sealed class MatchTransfersHandler(ITransactionRepository repository)
    : IRequestHandler<MatchTransfersCommand, Result<MatchTransfersResult>>
{
    public async Task<Result<MatchTransfersResult>> Handle(
        MatchTransfersCommand request,
        CancellationToken cancellationToken)
    {
        // Load all transactions across the user's bank accounts that aren't already matched.
        var all = new List<Domain.Entities.Transaction>();
        foreach (var accountId in request.BankAccountIds)
        {
            var txns = await repository.GetByBankAccountIdAsync(accountId, cancellationToken);
            all.AddRange(txns.Where(t => t.MatchedTransactionId is null));
        }

        if (request.RestrictToTransactionIds is { Count: > 0 } allowlist)
        {
            var allowedIds = allowlist.ToHashSet();
            all = all.Where(t => allowedIds.Contains(t.Id)).ToList();
        }

        var examined = all.Count;
        var matched = 0;
        var ambiguous = 0;
        var tolerance = TimeSpan.FromDays(Math.Max(0, request.DateToleranceDays));

        // Iterate debits; for each, look for a credit on a different account with same amount, currency, and within the tolerance window.
        var debits = all.Where(t => t.Direction == TransactionDirection.Debit).ToList();
        var creditPool = all.Where(t => t.Direction == TransactionDirection.Credit).ToList();

        foreach (var debit in debits)
        {
            if (debit.MatchedTransactionId is not null) continue;

            var candidates = creditPool
                .Where(c => c.MatchedTransactionId is null
                            && c.BankAccountId != debit.BankAccountId
                            && c.Amount.CurrencyCode == debit.Amount.CurrencyCode
                            && c.Amount.Amount == debit.Amount.Amount
                            && (c.TransactionDate - debit.TransactionDate).Duration() <= tolerance)
                .ToList();

            if (candidates.Count == 0) continue;
            if (candidates.Count > 1) { ambiguous++; continue; }

            var partner = candidates[0];
            debit.MatchTo(partner.Id);
            partner.MatchTo(debit.Id);
            await repository.UpdateAsync(debit, cancellationToken);
            await repository.UpdateAsync(partner, cancellationToken);
            matched++;
        }

        return Result<MatchTransfersResult>.Success(new MatchTransfersResult(examined, matched, ambiguous));
    }
}
