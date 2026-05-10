using FluentAssertions;
using FreeBudget.SharedKernel.ValueObjects;
using FreeBudget.Transactions.Application.Interfaces;
using FreeBudget.Transactions.Application.Queries;
using FreeBudget.Transactions.Domain.Entities;
using FreeBudget.Transactions.Domain.ValueObjects;
using NSubstitute;

namespace FreeBudget.Transactions.Application.Tests.Queries;

public class GetTransactionsByBankAccountHandlerTests
{
    private readonly ITransactionRepository _repo = Substitute.For<ITransactionRepository>();
    private readonly GetTransactionsByBankAccountHandler _handler;
    private static readonly Guid BankAccountId = Guid.NewGuid();

    public GetTransactionsByBankAccountHandlerTests()
    {
        _handler = new GetTransactionsByBankAccountHandler(_repo);
    }

    [Fact]
    public async Task Returns_empty_when_no_transactions()
    {
        _repo.GetByBankAccountIdAsync(BankAccountId, Arg.Any<CancellationToken>())
            .Returns(new List<Transaction>());

        var result = await _handler.Handle(
            new GetTransactionsByBankAccountQuery(BankAccountId),
            CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Maps_transaction_fields_to_dto()
    {
        var txn = Transaction.Create(
            BankAccountId, new DateTime(2024, 5, 15), "TESCO",
            new Money(25.50m, "GBP"), TransactionDirection.Debit,
            category: "Groceries", externalTransactionId: "ext-1");

        _repo.GetByBankAccountIdAsync(BankAccountId, Arg.Any<CancellationToken>())
            .Returns(new List<Transaction> { txn });

        var result = await _handler.Handle(
            new GetTransactionsByBankAccountQuery(BankAccountId),
            CancellationToken.None);

        result.Should().ContainSingle();
        var item = result[0];
        item.Id.Should().Be(txn.Id);
        item.BankAccountId.Should().Be(BankAccountId);
        item.TransactionDate.Should().Be(new DateTime(2024, 5, 15));
        item.Description.Should().Be("TESCO");
        item.Amount.Should().Be(25.50m);
        item.CurrencyCode.Should().Be("GBP");
        item.Direction.Should().Be("Debit");
        item.Category.Should().Be("Groceries");
        item.ExternalTransactionId.Should().Be("ext-1");
    }

    [Fact]
    public async Task Orders_by_transaction_date_descending()
    {
        var transactions = new List<Transaction>
        {
            Transaction.Create(BankAccountId, new DateTime(2024, 5, 1), "OLDEST", new Money(10m, "GBP"), TransactionDirection.Debit),
            Transaction.Create(BankAccountId, new DateTime(2024, 5, 15), "NEWEST", new Money(20m, "GBP"), TransactionDirection.Debit),
            Transaction.Create(BankAccountId, new DateTime(2024, 5, 8), "MIDDLE", new Money(15m, "GBP"), TransactionDirection.Debit),
        };

        _repo.GetByBankAccountIdAsync(BankAccountId, Arg.Any<CancellationToken>())
            .Returns(transactions);

        var result = await _handler.Handle(
            new GetTransactionsByBankAccountQuery(BankAccountId),
            CancellationToken.None);

        result.Select(r => r.Description).Should().Equal("NEWEST", "MIDDLE", "OLDEST");
    }

    [Fact]
    public async Task Uses_date_range_query_when_dates_provided()
    {
        var from = new DateTime(2024, 5, 1);
        var to = new DateTime(2024, 5, 31);

        _repo.GetByBankAccountIdAndDateRangeAsync(BankAccountId, from, to, Arg.Any<CancellationToken>())
            .Returns(new List<Transaction>());

        await _handler.Handle(
            new GetTransactionsByBankAccountQuery(BankAccountId, from, to),
            CancellationToken.None);

        await _repo.Received(1).GetByBankAccountIdAndDateRangeAsync(BankAccountId, from, to, Arg.Any<CancellationToken>());
        await _repo.DidNotReceive().GetByBankAccountIdAsync(BankAccountId, Arg.Any<CancellationToken>());
    }
}
