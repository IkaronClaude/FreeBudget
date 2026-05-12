using FluentAssertions;
using FreeBudget.Ledger.Application.Interfaces;
using FreeBudget.Ledger.Application.Queries;
using FreeBudget.Ledger.Domain.Entities;
using FreeBudget.SharedKernel.ValueObjects;
using NSubstitute;

namespace FreeBudget.Ledger.Application.Tests.Queries;

public class GetGroupBalancesHandlerTests
{
    private readonly ILedgerEntryRepository _repo = Substitute.For<ILedgerEntryRepository>();
    private readonly GetGroupBalancesHandler _handler;

    private static readonly Guid GroupId = Guid.NewGuid();
    private static readonly Guid MemberA = Guid.NewGuid();
    private static readonly Guid MemberB = Guid.NewGuid();
    private static readonly Guid MemberC = Guid.NewGuid();
    private static readonly Guid Creator = Guid.NewGuid();
    private static readonly DateTime Date = new(2024, 5, 15);

    public GetGroupBalancesHandlerTests()
    {
        _handler = new GetGroupBalancesHandler(_repo);
    }

    [Fact]
    public async Task Returns_empty_when_no_entries()
    {
        _repo.GetByGroupIdAsync(GroupId, Arg.Any<CancellationToken>())
            .Returns(new List<LedgerEntry>());

        var result = await _handler.Handle(new GetGroupBalancesQuery(GroupId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Single_expense_shows_debt()
    {
        var entries = new List<LedgerEntry>
        {
            LedgerEntry.CreateExpense(GroupId, MemberA, MemberB, new Money(50m, "GBP"), "Dinner", Date, Creator),
        };
        _repo.GetByGroupIdAsync(GroupId, Arg.Any<CancellationToken>()).Returns(entries);

        var result = await _handler.Handle(new GetGroupBalancesQuery(GroupId), CancellationToken.None);

        result.Should().ContainSingle();
        var balance = result[0];
        balance.MemberId.Should().Be(MemberB);
        balance.OwesToMemberId.Should().Be(MemberA);
        balance.NetAmount.Amount.Should().Be(50m);
    }

    [Fact]
    public async Task Multiple_expenses_same_pair_are_summed()
    {
        var entries = new List<LedgerEntry>
        {
            LedgerEntry.CreateExpense(GroupId, MemberA, MemberB, new Money(30m, "GBP"), "Lunch", Date, Creator),
            LedgerEntry.CreateExpense(GroupId, MemberA, MemberB, new Money(20m, "GBP"), "Coffee", Date, Creator),
        };
        _repo.GetByGroupIdAsync(GroupId, Arg.Any<CancellationToken>()).Returns(entries);

        var result = await _handler.Handle(new GetGroupBalancesQuery(GroupId), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].NetAmount.Amount.Should().Be(50m);
    }

    [Fact]
    public async Task Settlement_reduces_balance()
    {
        var entries = new List<LedgerEntry>
        {
            LedgerEntry.CreateExpense(GroupId, MemberA, MemberB, new Money(50m, "GBP"), "Dinner", Date, Creator),
            LedgerEntry.CreateSettlement(GroupId, MemberB, MemberA, new Money(20m, "GBP"), "Partial payback", Date, Creator),
        };
        _repo.GetByGroupIdAsync(GroupId, Arg.Any<CancellationToken>()).Returns(entries);

        var result = await _handler.Handle(new GetGroupBalancesQuery(GroupId), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].MemberId.Should().Be(MemberB);
        result[0].OwesToMemberId.Should().Be(MemberA);
        result[0].NetAmount.Amount.Should().Be(30m);
    }

    [Fact]
    public async Task Full_settlement_returns_empty()
    {
        var entries = new List<LedgerEntry>
        {
            LedgerEntry.CreateExpense(GroupId, MemberA, MemberB, new Money(50m, "GBP"), "Dinner", Date, Creator),
            LedgerEntry.CreateSettlement(GroupId, MemberB, MemberA, new Money(50m, "GBP"), "Full payback", Date, Creator),
        };
        _repo.GetByGroupIdAsync(GroupId, Arg.Any<CancellationToken>()).Returns(entries);

        var result = await _handler.Handle(new GetGroupBalancesQuery(GroupId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Bidirectional_expenses_net_out()
    {
        var entries = new List<LedgerEntry>
        {
            LedgerEntry.CreateExpense(GroupId, MemberA, MemberB, new Money(30m, "GBP"), "Lunch", Date, Creator),
            LedgerEntry.CreateExpense(GroupId, MemberB, MemberA, new Money(20m, "GBP"), "Coffee", Date, Creator),
        };
        _repo.GetByGroupIdAsync(GroupId, Arg.Any<CancellationToken>()).Returns(entries);

        var result = await _handler.Handle(new GetGroupBalancesQuery(GroupId), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].MemberId.Should().Be(MemberB);
        result[0].OwesToMemberId.Should().Be(MemberA);
        result[0].NetAmount.Amount.Should().Be(10m);
    }

    [Fact]
    public async Task Multiple_participants_create_separate_balances()
    {
        var entries = new List<LedgerEntry>
        {
            LedgerEntry.CreateExpense(GroupId, MemberA, MemberB, new Money(30m, "GBP"), "Lunch", Date, Creator),
            LedgerEntry.CreateExpense(GroupId, MemberA, MemberC, new Money(20m, "GBP"), "Coffee", Date, Creator),
        };
        _repo.GetByGroupIdAsync(GroupId, Arg.Any<CancellationToken>()).Returns(entries);

        var result = await _handler.Handle(new GetGroupBalancesQuery(GroupId), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().Contain(b => b.MemberId == MemberB && b.OwesToMemberId == MemberA && b.NetAmount.Amount == 30m);
        result.Should().Contain(b => b.MemberId == MemberC && b.OwesToMemberId == MemberA && b.NetAmount.Amount == 20m);
    }
}
