using FluentAssertions;
using FreeBudget.Ledger.Application.Commands;
using FreeBudget.Ledger.Application.Interfaces;
using FreeBudget.Ledger.Domain.Entities;
using FreeBudget.Ledger.Domain.Enums;
using NSubstitute;

namespace FreeBudget.Ledger.Application.Tests.Commands;

public class SplitTransactionHandlerTests
{
    private readonly ILedgerEntryRepository _repo = Substitute.For<ILedgerEntryRepository>();
    private readonly SplitTransactionHandler _handler;

    private static readonly Guid GroupId = Guid.NewGuid();
    private static readonly Guid Payer = Guid.NewGuid();
    private static readonly Guid UserB = Guid.NewGuid();
    private static readonly Guid UserC = Guid.NewGuid();
    private static readonly Guid TransactionId = Guid.NewGuid();
    private static readonly DateTime Date = new(2024, 5, 15);

    public SplitTransactionHandlerTests()
    {
        _handler = new SplitTransactionHandler(_repo);
        _repo.GetByTransactionIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<LedgerEntry>());
    }

    private static SplitTransactionCommand BuildCommand(params SplitParticipant[] participants) =>
        new(GroupId, Payer, TransactionId, "GBP", "Dinner", Date, Payer, participants);

    [Fact]
    public async Task Creates_one_entry_per_participant()
    {
        var command = BuildCommand(new SplitParticipant(UserB, 20m), new SplitParticipant(UserC, 20m));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCount(2);

        await _repo.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<LedgerEntry>>(entries => entries.Count() == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task All_entries_link_to_transaction_and_payer()
    {
        var command = BuildCommand(new SplitParticipant(UserB, 15m), new SplitParticipant(UserC, 25m));

        await _handler.Handle(command, CancellationToken.None);

        await _repo.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<LedgerEntry>>(entries =>
                entries.All(e =>
                    e.TransactionId == TransactionId &&
                    e.PaidByUserId == Payer &&
                    e.GroupId == GroupId &&
                    e.EntryType == LedgerEntryType.Expense)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Each_entry_owed_by_correct_participant_with_correct_amount()
    {
        var command = BuildCommand(new SplitParticipant(UserB, 15m), new SplitParticipant(UserC, 25m));

        await _handler.Handle(command, CancellationToken.None);

        await _repo.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<LedgerEntry>>(entries =>
                entries.Any(e => e.OwedByUserId == UserB && e.Amount.Amount == 15m) &&
                entries.Any(e => e.OwedByUserId == UserC && e.Amount.Amount == 25m)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Empty_participants_fails()
    {
        var command = BuildCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repo.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<LedgerEntry>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Empty_transaction_id_fails()
    {
        var command = new SplitTransactionCommand(
            GroupId, Payer, Guid.Empty, "GBP", "Dinner", Date, Payer,
            new[] { new SplitParticipant(UserB, 20m) });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repo.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<LedgerEntry>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Payer_in_participants_fails()
    {
        var command = BuildCommand(new SplitParticipant(UserB, 20m), new SplitParticipant(Payer, 20m));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repo.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<LedgerEntry>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Non_positive_amount_fails()
    {
        var command = BuildCommand(new SplitParticipant(UserB, 0m));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Duplicate_participants_fails()
    {
        var command = BuildCommand(new SplitParticipant(UserB, 10m), new SplitParticipant(UserB, 20m));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repo.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<LedgerEntry>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Already_split_transaction_fails()
    {
        var existingEntry = LedgerEntry.CreateExpense(
            GroupId, Payer, UserB,
            new SharedKernel.ValueObjects.Money(10m, "GBP"),
            "Earlier split", Date, Payer, TransactionId);

        _repo.GetByTransactionIdAsync(TransactionId, Arg.Any<CancellationToken>())
            .Returns(new[] { existingEntry });

        var command = BuildCommand(new SplitParticipant(UserB, 20m));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repo.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<LedgerEntry>>(), Arg.Any<CancellationToken>());
    }
}
