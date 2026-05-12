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
    private static readonly Guid MemberB = Guid.NewGuid();
    private static readonly Guid MemberC = Guid.NewGuid();
    private static readonly Guid Creator = Guid.NewGuid();
    private static readonly Guid TransactionId = Guid.NewGuid();
    private static readonly DateTime Date = new(2024, 5, 15);

    public SplitTransactionHandlerTests()
    {
        _handler = new SplitTransactionHandler(_repo);
        _repo.GetByTransactionIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<LedgerEntry>());
    }

    private static SplitTransactionCommand BuildCommand(params SplitParticipant[] participants) =>
        new(GroupId, Payer, TransactionId, "GBP", "Dinner", Date, Creator, participants);

    [Fact]
    public async Task Creates_one_entry_per_participant()
    {
        var command = BuildCommand(new SplitParticipant(MemberB, 20m), new SplitParticipant(MemberC, 20m));

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
        var command = BuildCommand(new SplitParticipant(MemberB, 15m), new SplitParticipant(MemberC, 25m));

        await _handler.Handle(command, CancellationToken.None);

        await _repo.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<LedgerEntry>>(entries =>
                entries.All(e =>
                    e.TransactionId == TransactionId &&
                    e.PaidByMemberId == Payer &&
                    e.GroupId == GroupId &&
                    e.EntryType == LedgerEntryType.Expense)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Each_entry_owed_by_correct_participant_with_correct_amount()
    {
        var command = BuildCommand(new SplitParticipant(MemberB, 15m), new SplitParticipant(MemberC, 25m));

        await _handler.Handle(command, CancellationToken.None);

        await _repo.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<LedgerEntry>>(entries =>
                Enumerable.Any(entries, e => e.OwedByMemberId == MemberB && e.Amount.Amount == 15m) &&
                Enumerable.Any(entries, e => e.OwedByMemberId == MemberC && e.Amount.Amount == 25m)),
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
            GroupId, Payer, Guid.Empty, "GBP", "Dinner", Date, Creator,
            new[] { new SplitParticipant(MemberB, 20m) });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repo.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<LedgerEntry>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Payer_in_participants_fails()
    {
        var command = BuildCommand(new SplitParticipant(MemberB, 20m), new SplitParticipant(Payer, 20m));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repo.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<LedgerEntry>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Non_positive_amount_fails()
    {
        var command = BuildCommand(new SplitParticipant(MemberB, 0m));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Duplicate_participants_fails()
    {
        var command = BuildCommand(new SplitParticipant(MemberB, 10m), new SplitParticipant(MemberB, 20m));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repo.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<LedgerEntry>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Already_split_transaction_fails()
    {
        var existingEntry = LedgerEntry.CreateExpense(
            GroupId, Payer, MemberB,
            new SharedKernel.ValueObjects.Money(10m, "GBP"),
            "Earlier split", Date, Creator, TransactionId);

        _repo.GetByTransactionIdAsync(TransactionId, Arg.Any<CancellationToken>())
            .Returns(new[] { existingEntry });

        var command = BuildCommand(new SplitParticipant(MemberB, 20m));

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _repo.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<LedgerEntry>>(), Arg.Any<CancellationToken>());
    }
}
