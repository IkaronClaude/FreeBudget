using System.Globalization;
using FreeBudget.Transactions.Application.DTOs;
using FreeBudget.Transactions.Application.Interfaces;
using FreeBudget.Transactions.Domain.Entities;
using FreeBudget.Transactions.Domain.ValueObjects;
using MediatR;

namespace FreeBudget.Transactions.Application.Queries;

public sealed record GetPeriodBreakdownQuery(
    Guid BankAccountId,
    DateTime From,
    DateTime To,
    PeriodGranularity Granularity) : IRequest<IReadOnlyList<PeriodBreakdownItem>>;

internal sealed class GetPeriodBreakdownHandler(
    ITransactionRepository repository)
    : IRequestHandler<GetPeriodBreakdownQuery, IReadOnlyList<PeriodBreakdownItem>>
{
    public async Task<IReadOnlyList<PeriodBreakdownItem>> Handle(
        GetPeriodBreakdownQuery request,
        CancellationToken cancellationToken)
    {
        var transactions = await repository.GetByBankAccountIdAndDateRangeAsync(
            request.BankAccountId, request.From, request.To, cancellationToken);

        return transactions
            .GroupBy(t => GetPeriodStart(t, request.Granularity))
            .OrderBy(g => g.Key)
            .Select(g => new PeriodBreakdownItem(
                PeriodLabel: FormatPeriodLabel(g.Key, request.Granularity),
                PeriodStart: g.Key,
                TotalCredit: g.Where(t => t.Direction == TransactionDirection.Credit).Sum(t => t.Amount.Amount),
                TotalDebit: g.Where(t => t.Direction == TransactionDirection.Debit).Sum(t => t.Amount.Amount),
                Net: g.Where(t => t.Direction == TransactionDirection.Credit).Sum(t => t.Amount.Amount)
                   - g.Where(t => t.Direction == TransactionDirection.Debit).Sum(t => t.Amount.Amount),
                TransactionCount: g.Count()))
            .ToList();
    }

    private static DateTime GetPeriodStart(Transaction transaction, PeriodGranularity granularity)
    {
        var date = transaction.TransactionDate.Date;
        return granularity switch
        {
            PeriodGranularity.Day => date,
            PeriodGranularity.Week => date.AddDays(-(int)date.DayOfWeek + (int)DayOfWeek.Monday),
            PeriodGranularity.Month => new DateTime(date.Year, date.Month, 1),
            _ => date,
        };
    }

    private static string FormatPeriodLabel(DateTime periodStart, PeriodGranularity granularity)
    {
        return granularity switch
        {
            PeriodGranularity.Day => periodStart.ToString("yyyy-MM-dd"),
            PeriodGranularity.Week => $"W{ISOWeek.GetWeekOfYear(periodStart):D2} {periodStart:yyyy-MM-dd}",
            PeriodGranularity.Month => periodStart.ToString("yyyy-MM"),
            _ => periodStart.ToString("yyyy-MM-dd"),
        };
    }
}
