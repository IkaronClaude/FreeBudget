using FluentAssertions;
using FreeBudget.Ledger.Domain.Entities;
using FreeBudget.Ledger.Domain.Enums;
using FreeBudget.Ledger.Domain.Events;
using FreeBudget.SharedKernel.ValueObjects;

namespace FreeBudget.Ledger.Domain.Tests.Entities;

public class LedgerEntryTests
{
    private static readonly Guid GroupId = Guid.NewGuid();
    private static readonly Guid UserA = Guid.NewGuid();
    private static readonly Guid UserB = Guid.NewGuid();
    private static readonly Guid Creator = Guid.NewGuid();
    private static readonly Money FiveQuid = new(5m, "GBP");
    private static readonly DateTime EntryDate = new(2024, 5, 15);

    [Fact]
    public void CreateExpense_sets_all_properties()
    {
        var entry = LedgerEntry.CreateExpense(GroupId, UserA, UserB, FiveQuid, "Lunch", EntryDate, Creator);

        entry.Id.Should().NotBeEmpty();
        entry.GroupId.Should().Be(GroupId);
        entry.PaidByUserId.Should().Be(UserA);
        entry.OwedByUserId.Should().Be(UserB);
        entry.Amount.Should().Be(FiveQuid);
        entry.Description.Should().Be("Lunch");
        entry.EntryType.Should().Be(LedgerEntryType.Expense);
        entry.EntryDate.Should().Be(EntryDate);
        entry.CreatedByUserId.Should().Be(Creator);
        entry.TransactionId.Should().BeNull();
    }

    [Fact]
    public void CreateSettlement_sets_entry_type_to_settlement()
    {
        var entry = LedgerEntry.CreateSettlement(GroupId, UserB, UserA, FiveQuid, "Paying back", EntryDate, Creator);

        entry.EntryType.Should().Be(LedgerEntryType.Settlement);
    }

    [Fact]
    public void CreateExpense_with_transaction_id()
    {
        var txnId = Guid.NewGuid();
        var entry = LedgerEntry.CreateExpense(GroupId, UserA, UserB, FiveQuid, "Lunch", EntryDate, Creator, txnId);

        entry.TransactionId.Should().Be(txnId);
    }

    [Fact]
    public void CreateExpense_trims_description()
    {
        var entry = LedgerEntry.CreateExpense(GroupId, UserA, UserB, FiveQuid, "  Lunch  ", EntryDate, Creator);

        entry.Description.Should().Be("Lunch");
    }

    [Fact]
    public void CreateExpense_with_empty_group_id_throws()
    {
        var act = () => LedgerEntry.CreateExpense(Guid.Empty, UserA, UserB, FiveQuid, "Lunch", EntryDate, Creator);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateExpense_with_empty_paid_by_throws()
    {
        var act = () => LedgerEntry.CreateExpense(GroupId, Guid.Empty, UserB, FiveQuid, "Lunch", EntryDate, Creator);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateExpense_with_empty_owed_by_throws()
    {
        var act = () => LedgerEntry.CreateExpense(GroupId, UserA, Guid.Empty, FiveQuid, "Lunch", EntryDate, Creator);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateExpense_with_same_paid_and_owed_throws()
    {
        var act = () => LedgerEntry.CreateExpense(GroupId, UserA, UserA, FiveQuid, "Lunch", EntryDate, Creator);

        act.Should().Throw<ArgumentException>().WithMessage("*owes themselves*");
    }

    [Fact]
    public void CreateExpense_with_zero_amount_throws()
    {
        var act = () => LedgerEntry.CreateExpense(GroupId, UserA, UserB, new Money(0m, "GBP"), "Lunch", EntryDate, Creator);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateExpense_with_negative_amount_throws()
    {
        var act = () => LedgerEntry.CreateExpense(GroupId, UserA, UserB, new Money(-5m, "GBP"), "Lunch", EntryDate, Creator);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateExpense_with_null_amount_throws()
    {
        var act = () => LedgerEntry.CreateExpense(GroupId, UserA, UserB, null!, "Lunch", EntryDate, Creator);

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void CreateExpense_with_invalid_description_throws(string? description)
    {
        var act = () => LedgerEntry.CreateExpense(GroupId, UserA, UserB, FiveQuid, description!, EntryDate, Creator);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateExpense_with_empty_creator_throws()
    {
        var act = () => LedgerEntry.CreateExpense(GroupId, UserA, UserB, FiveQuid, "Lunch", EntryDate, Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateExpense_raises_LedgerEntryCreatedEvent()
    {
        var entry = LedgerEntry.CreateExpense(GroupId, UserA, UserB, FiveQuid, "Lunch", EntryDate, Creator);

        entry.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<LedgerEntryCreatedEvent>()
            .Which.Should().BeEquivalentTo(new
            {
                EntryId = entry.Id,
                GroupId,
                PaidByUserId = UserA,
                OwedByUserId = UserB,
                Amount = 5m,
                CurrencyCode = "GBP",
                EntryType = LedgerEntryType.Expense,
            });
    }

    [Fact]
    public void LinkTransaction_sets_transaction_id()
    {
        var entry = LedgerEntry.CreateExpense(GroupId, UserA, UserB, FiveQuid, "Lunch", EntryDate, Creator);
        var txnId = Guid.NewGuid();

        entry.LinkTransaction(txnId);

        entry.TransactionId.Should().Be(txnId);
    }

    [Fact]
    public void LinkTransaction_with_empty_id_throws()
    {
        var entry = LedgerEntry.CreateExpense(GroupId, UserA, UserB, FiveQuid, "Lunch", EntryDate, Creator);

        var act = () => entry.LinkTransaction(Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }
}
