using FluentAssertions;
using FreeBudget.Ledger.Application.Commands;
using FreeBudget.Ledger.Application.Interfaces;
using FreeBudget.Ledger.Domain.Entities;
using FreeBudget.Ledger.Domain.Enums;
using NSubstitute;

namespace FreeBudget.Ledger.Application.Tests.Commands;

public class CreateExpenseHandlerTests
{
    private readonly ILedgerEntryRepository _repo = Substitute.For<ILedgerEntryRepository>();
    private readonly CreateExpenseHandler _handler;

    private static readonly Guid GroupId = Guid.NewGuid();
    private static readonly Guid UserA = Guid.NewGuid();
    private static readonly Guid UserB = Guid.NewGuid();
    private static readonly DateTime Date = new(2024, 5, 15);

    public CreateExpenseHandlerTests()
    {
        _handler = new CreateExpenseHandler(_repo);
    }

    [Fact]
    public async Task Creates_expense_and_returns_id()
    {
        var command = new CreateExpenseCommand(GroupId, UserA, UserB, 25m, "GBP", "Lunch", Date, UserA);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        await _repo.Received(1).AddAsync(
            Arg.Is<LedgerEntry>(e =>
                e.GroupId == GroupId &&
                e.PaidByUserId == UserA &&
                e.OwedByUserId == UserB &&
                e.Amount.Amount == 25m &&
                e.EntryType == LedgerEntryType.Expense),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Creates_expense_with_transaction_id()
    {
        var txnId = Guid.NewGuid();
        var command = new CreateExpenseCommand(GroupId, UserA, UserB, 10m, "GBP", "Coffee", Date, UserA, txnId);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repo.Received(1).AddAsync(
            Arg.Is<LedgerEntry>(e => e.TransactionId == txnId),
            Arg.Any<CancellationToken>());
    }
}
