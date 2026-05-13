using FluentAssertions;
using FreeBudget.SharedKernel.ValueObjects;
using FreeBudget.Transactions.Application.Commands;
using FreeBudget.Transactions.Application.Interfaces;
using FreeBudget.Transactions.Domain.Entities;
using FreeBudget.Transactions.Domain.ValueObjects;
using NSubstitute;

namespace FreeBudget.Transactions.Application.Tests.Commands;

public class BulkUpdateTransactionCategoryHandlerTests
{
    private readonly ITransactionRepository _repo = Substitute.For<ITransactionRepository>();
    private readonly BulkUpdateTransactionCategoryHandler _handler;
    private static readonly Guid BankAccountId = Guid.NewGuid();

    public BulkUpdateTransactionCategoryHandlerTests()
    {
        _handler = new BulkUpdateTransactionCategoryHandler(_repo);
    }

    private static Transaction MakeTxn() =>
        Transaction.Create(BankAccountId, DateTime.UtcNow.Date, "TESCO", new Money(10m, "GBP"), TransactionDirection.Debit);

    [Fact]
    public async Task Empty_ids_returns_zero_without_repo_calls()
    {
        var result = await _handler.Handle(
            new BulkUpdateTransactionCategoryCommand([], "Groceries"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Updated.Should().Be(0);
        result.Value!.NotFound.Should().Be(0);
        await _repo.DidNotReceive().GetByIdsAsync(Arg.Any<IReadOnlyList<Guid>>(), Arg.Any<CancellationToken>());
        await _repo.DidNotReceive().UpdateRangeAsync(Arg.Any<IEnumerable<Transaction>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Updates_each_found_transaction_and_saves_once()
    {
        var t1 = MakeTxn();
        var t2 = MakeTxn();
        var ids = new[] { t1.Id, t2.Id };
        _repo.GetByIdsAsync(ids, Arg.Any<CancellationToken>()).Returns(new List<Transaction> { t1, t2 });

        var result = await _handler.Handle(
            new BulkUpdateTransactionCategoryCommand(ids, "Groceries"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Updated.Should().Be(2);
        result.Value!.NotFound.Should().Be(0);
        t1.Category.Should().Be("Groceries");
        t2.Category.Should().Be("Groceries");
        await _repo.Received(1).UpdateRangeAsync(Arg.Any<IEnumerable<Transaction>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Missing_ids_are_reported_as_NotFound()
    {
        var t1 = MakeTxn();
        var missing = Guid.NewGuid();
        var ids = new[] { t1.Id, missing };
        _repo.GetByIdsAsync(ids, Arg.Any<CancellationToken>()).Returns(new List<Transaction> { t1 });

        var result = await _handler.Handle(
            new BulkUpdateTransactionCategoryCommand(ids, "Bills"),
            CancellationToken.None);

        result.Value!.Updated.Should().Be(1);
        result.Value!.NotFound.Should().Be(1);
    }

    [Fact]
    public async Task Whitespace_category_clears_categories()
    {
        var t1 = MakeTxn();
        t1.UpdateCategory("Groceries");
        var ids = new[] { t1.Id };
        _repo.GetByIdsAsync(ids, Arg.Any<CancellationToken>()).Returns(new List<Transaction> { t1 });

        var result = await _handler.Handle(
            new BulkUpdateTransactionCategoryCommand(ids, "   "),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        t1.Category.Should().BeNull();
    }
}
