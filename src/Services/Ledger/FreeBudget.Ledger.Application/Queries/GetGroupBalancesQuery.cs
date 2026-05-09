using FreeBudget.Ledger.Application.DTOs;
using FreeBudget.Ledger.Application.Interfaces;
using FreeBudget.Ledger.Domain.Enums;
using FreeBudget.SharedKernel.ValueObjects;
using MediatR;

namespace FreeBudget.Ledger.Application.Queries;

public sealed record GetGroupBalancesQuery(Guid GroupId)
    : IRequest<IReadOnlyList<UserBalance>>;

internal sealed class GetGroupBalancesHandler(ILedgerEntryRepository repository)
    : IRequestHandler<GetGroupBalancesQuery, IReadOnlyList<UserBalance>>
{
    public async Task<IReadOnlyList<UserBalance>> Handle(
        GetGroupBalancesQuery request,
        CancellationToken cancellationToken)
    {
        var entries = await repository.GetByGroupIdAsync(request.GroupId, cancellationToken);

        if (entries.Count == 0)
            return [];

        var debts = new Dictionary<(Guid OwedBy, Guid OwedTo), decimal>();

        foreach (var entry in entries)
        {
            var key = (entry.OwedByUserId, entry.PaidByUserId);
            debts.TryGetValue(key, out var current);

            debts[key] = entry.EntryType == LedgerEntryType.Expense
                ? current + entry.Amount.Amount
                : current - entry.Amount.Amount;
        }

        var netDebts = new Dictionary<(Guid, Guid), decimal>();

        foreach (var ((owedBy, owedTo), amount) in debts)
        {
            var reverseKey = (owedTo, owedBy);
            debts.TryGetValue(reverseKey, out var reverseAmount);

            var normalizedKey = owedBy.CompareTo(owedTo) < 0
                ? (owedBy, owedTo)
                : (owedTo, owedBy);

            if (!netDebts.ContainsKey(normalizedKey))
            {
                var net = owedBy.CompareTo(owedTo) < 0
                    ? amount - reverseAmount
                    : reverseAmount - amount;

                netDebts[normalizedKey] = net;
            }
        }

        var currency = entries[0].Amount.CurrencyCode;
        var balances = new List<UserBalance>();

        foreach (var ((userA, userB), net) in netDebts)
        {
            if (net == 0) continue;

            balances.Add(net > 0
                ? new UserBalance(userA, userB, new Money(net, currency))
                : new UserBalance(userB, userA, new Money(-net, currency)));
        }

        return balances;
    }
}
