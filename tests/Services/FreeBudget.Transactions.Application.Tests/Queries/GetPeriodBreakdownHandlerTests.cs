using FluentAssertions;
using FreeBudget.Transactions.Application.DTOs;
using FreeBudget.Transactions.Application.Interfaces;
using FreeBudget.Transactions.Application.Queries;
using FreeBudget.Transactions.Domain.Entities;
using FreeBudget.Transactions.Domain.ValueObjects;
using NSubstitute;

namespace FreeBudget.Transactions.Application.Tests.Queries;

public class GetPeriodBreakdownHandlerTests
{
    private readonly ITransactionRepository _repo = Substitute.For<ITransactionRepository>();
    private readonly GetPeriodBreakdownHandler _handler;
    private static readonly Guid BankAccountId = Guid.NewGuid();

    public GetPeriodBreakdownHandlerTests()
    {
        _handler = new GetPeriodBreakdownHandler(_repo);
    }

    [Fact]
    public async Task Returns_empty_when_no_transactions()
    {
        _repo.GetByBankAccountIdAndDateRangeAsync(BankAccountId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<Transaction>());

        var result = await _handler.Handle(
            new GetPeriodBreakdownQuery(BankAccountId, DateTime.MinValue, DateTime.MaxValue, PeriodGranularity.Month),
            CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Groups_by_day()
    {
        var transactions = new List<Transaction>
        {
            Transaction.Create(BankAccountId, new DateTime(2024, 5, 1), "A", new Money(10m, "GBP"), TransactionDirection.Debit),
            Transaction.Create(BankAccountId, new DateTime(2024, 5, 1), "B", new Money(20m, "GBP"), TransactionDirection.Credit),
            Transaction.Create(BankAccountId, new DateTime(2024, 5, 2), "C", new Money(30m, "GBP"), TransactionDirection.Debit),
        };

        _repo.GetByBankAccountIdAndDateRangeAsync(BankAccountId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(transactions);

        var result = await _handler.Handle(
            new GetPeriodBreakdownQuery(BankAccountId, new DateTime(2024, 5, 1), new DateTime(2024, 5, 31), PeriodGranularity.Day),
            CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].PeriodLabel.Should().Be("2024-05-01");
        result[0].TotalDebit.Should().Be(10m);
        result[0].TotalCredit.Should().Be(20m);
        result[0].Net.Should().Be(10m);
        result[0].TransactionCount.Should().Be(2);

        result[1].PeriodLabel.Should().Be("2024-05-02");
        result[1].TotalDebit.Should().Be(30m);
    }

    [Fact]
    public async Task Groups_by_month()
    {
        var transactions = new List<Transaction>
        {
            Transaction.Create(BankAccountId, new DateTime(2024, 5, 1), "May1", new Money(100m, "GBP"), TransactionDirection.Debit),
            Transaction.Create(BankAccountId, new DateTime(2024, 5, 15), "May2", new Money(50m, "GBP"), TransactionDirection.Debit),
            Transaction.Create(BankAccountId, new DateTime(2024, 6, 1), "Jun1", new Money(200m, "GBP"), TransactionDirection.Debit),
        };

        _repo.GetByBankAccountIdAndDateRangeAsync(BankAccountId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(transactions);

        var result = await _handler.Handle(
            new GetPeriodBreakdownQuery(BankAccountId, new DateTime(2024, 5, 1), new DateTime(2024, 6, 30), PeriodGranularity.Month),
            CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].PeriodLabel.Should().Be("2024-05");
        result[0].TotalDebit.Should().Be(150m);
        result[0].TransactionCount.Should().Be(2);

        result[1].PeriodLabel.Should().Be("2024-06");
        result[1].TotalDebit.Should().Be(200m);
        result[1].TransactionCount.Should().Be(1);
    }

    [Fact]
    public async Task Groups_by_week()
    {
        // 2024-05-06 is a Monday
        var transactions = new List<Transaction>
        {
            Transaction.Create(BankAccountId, new DateTime(2024, 5, 6), "Mon", new Money(10m, "GBP"), TransactionDirection.Debit),
            Transaction.Create(BankAccountId, new DateTime(2024, 5, 10), "Fri", new Money(20m, "GBP"), TransactionDirection.Debit),
            Transaction.Create(BankAccountId, new DateTime(2024, 5, 13), "NextMon", new Money(30m, "GBP"), TransactionDirection.Debit),
        };

        _repo.GetByBankAccountIdAndDateRangeAsync(BankAccountId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(transactions);

        var result = await _handler.Handle(
            new GetPeriodBreakdownQuery(BankAccountId, new DateTime(2024, 5, 1), new DateTime(2024, 5, 31), PeriodGranularity.Week),
            CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].PeriodStart.Should().Be(new DateTime(2024, 5, 6));
        result[0].TotalDebit.Should().Be(30m);
        result[0].TransactionCount.Should().Be(2);

        result[1].PeriodStart.Should().Be(new DateTime(2024, 5, 13));
        result[1].TotalDebit.Should().Be(30m);
        result[1].TransactionCount.Should().Be(1);
    }

    [Fact]
    public async Task Results_ordered_chronologically()
    {
        var transactions = new List<Transaction>
        {
            Transaction.Create(BankAccountId, new DateTime(2024, 6, 1), "Later", new Money(10m, "GBP"), TransactionDirection.Debit),
            Transaction.Create(BankAccountId, new DateTime(2024, 5, 1), "Earlier", new Money(20m, "GBP"), TransactionDirection.Debit),
        };

        _repo.GetByBankAccountIdAndDateRangeAsync(BankAccountId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(transactions);

        var result = await _handler.Handle(
            new GetPeriodBreakdownQuery(BankAccountId, new DateTime(2024, 5, 1), new DateTime(2024, 6, 30), PeriodGranularity.Month),
            CancellationToken.None);

        result[0].PeriodLabel.Should().Be("2024-05");
        result[1].PeriodLabel.Should().Be("2024-06");
    }
}
