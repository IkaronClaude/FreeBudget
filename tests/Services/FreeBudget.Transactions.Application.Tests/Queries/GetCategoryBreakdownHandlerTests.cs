using FluentAssertions;
using FreeBudget.Transactions.Application.Interfaces;
using FreeBudget.Transactions.Application.Queries;
using FreeBudget.Transactions.Domain.Entities;
using FreeBudget.Transactions.Domain.ValueObjects;
using NSubstitute;

namespace FreeBudget.Transactions.Application.Tests.Queries;

public class GetCategoryBreakdownHandlerTests
{
    private readonly ITransactionRepository _repo = Substitute.For<ITransactionRepository>();
    private readonly GetCategoryBreakdownHandler _handler;
    private static readonly Guid BankAccountId = Guid.NewGuid();

    public GetCategoryBreakdownHandlerTests()
    {
        _handler = new GetCategoryBreakdownHandler(_repo);
    }

    [Fact]
    public async Task Returns_empty_when_no_transactions()
    {
        _repo.GetByBankAccountIdAndDateRangeAsync(BankAccountId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<Transaction>());

        var result = await _handler.Handle(
            new GetCategoryBreakdownQuery(BankAccountId, DateTime.MinValue, DateTime.MaxValue),
            CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Groups_by_category_with_totals()
    {
        var transactions = new List<Transaction>
        {
            Transaction.Create(BankAccountId, new DateTime(2024, 5, 1), "TESCO", new Money(25m, "GBP"), TransactionDirection.Debit, category: "Groceries"),
            Transaction.Create(BankAccountId, new DateTime(2024, 5, 2), "ALDI", new Money(30m, "GBP"), TransactionDirection.Debit, category: "Groceries"),
            Transaction.Create(BankAccountId, new DateTime(2024, 5, 3), "NETFLIX", new Money(10m, "GBP"), TransactionDirection.Debit, category: "Entertainment"),
            Transaction.Create(BankAccountId, new DateTime(2024, 5, 4), "SALARY", new Money(2000m, "GBP"), TransactionDirection.Credit, category: "Income"),
        };

        _repo.GetByBankAccountIdAndDateRangeAsync(BankAccountId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(transactions);

        var result = await _handler.Handle(
            new GetCategoryBreakdownQuery(BankAccountId, new DateTime(2024, 5, 1), new DateTime(2024, 5, 31)),
            CancellationToken.None);

        result.Should().HaveCount(3);

        var groceries = result.First(r => r.Category == "Groceries");
        groceries.TotalDebit.Should().Be(55m);
        groceries.TotalCredit.Should().Be(0m);
        groceries.Net.Should().Be(-55m);
        groceries.TransactionCount.Should().Be(2);

        var income = result.First(r => r.Category == "Income");
        income.TotalCredit.Should().Be(2000m);
        income.TotalDebit.Should().Be(0m);
        income.Net.Should().Be(2000m);
    }

    [Fact]
    public async Task Uncategorized_transactions_grouped_together()
    {
        var transactions = new List<Transaction>
        {
            Transaction.Create(BankAccountId, new DateTime(2024, 5, 1), "RANDOM SHOP", new Money(15m, "GBP"), TransactionDirection.Debit),
            Transaction.Create(BankAccountId, new DateTime(2024, 5, 2), "OTHER SHOP", new Money(20m, "GBP"), TransactionDirection.Debit),
        };

        _repo.GetByBankAccountIdAndDateRangeAsync(BankAccountId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(transactions);

        var result = await _handler.Handle(
            new GetCategoryBreakdownQuery(BankAccountId, new DateTime(2024, 5, 1), new DateTime(2024, 5, 31)),
            CancellationToken.None);

        result.Should().ContainSingle()
            .Which.Category.Should().Be("Uncategorized");
        result[0].TotalDebit.Should().Be(35m);
        result[0].TransactionCount.Should().Be(2);
    }

    [Fact]
    public async Task Results_ordered_by_total_debit_descending()
    {
        var transactions = new List<Transaction>
        {
            Transaction.Create(BankAccountId, new DateTime(2024, 5, 1), "SMALL", new Money(5m, "GBP"), TransactionDirection.Debit, category: "Small"),
            Transaction.Create(BankAccountId, new DateTime(2024, 5, 2), "BIG", new Money(500m, "GBP"), TransactionDirection.Debit, category: "Big"),
            Transaction.Create(BankAccountId, new DateTime(2024, 5, 3), "MED", new Money(50m, "GBP"), TransactionDirection.Debit, category: "Medium"),
        };

        _repo.GetByBankAccountIdAndDateRangeAsync(BankAccountId, Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(transactions);

        var result = await _handler.Handle(
            new GetCategoryBreakdownQuery(BankAccountId, new DateTime(2024, 5, 1), new DateTime(2024, 5, 31)),
            CancellationToken.None);

        result[0].Category.Should().Be("Big");
        result[1].Category.Should().Be("Medium");
        result[2].Category.Should().Be("Small");
    }
}
