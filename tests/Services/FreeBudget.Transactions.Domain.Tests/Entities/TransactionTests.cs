using FluentAssertions;
using FreeBudget.Transactions.Domain.Entities;
using FreeBudget.Transactions.Domain.Events;
using FreeBudget.Transactions.Domain.ValueObjects;

namespace FreeBudget.Transactions.Domain.Tests.Entities;

public class TransactionTests
{
    private static readonly Guid BankAccountId = Guid.NewGuid();
    private static readonly Money TestAmount = new(25.50m, "GBP");
    private static readonly TransactionDirection TestDirection = TransactionDirection.Debit;

    [Fact]
    public void Create_sets_all_properties()
    {
        var date = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        var transaction = Transaction.Create(
            BankAccountId, date, "Coffee shop", TestAmount, TestDirection);

        transaction.Id.Should().NotBeEmpty();
        transaction.BankAccountId.Should().Be(BankAccountId);
        transaction.TransactionDate.Should().Be(date);
        transaction.Description.Should().Be("Coffee shop");
        transaction.Amount.Should().Be(TestAmount);
        transaction.Direction.Should().Be(TestDirection);
        transaction.RunningBalance.Should().BeNull();
        transaction.ExternalTransactionId.Should().BeNull();
        transaction.ImportBatchId.Should().BeNull();
    }

    [Fact]
    public void Create_with_optional_fields_sets_them()
    {
        var balance = new Money(100m, "GBP");
        var batchId = Guid.NewGuid();

        var transaction = Transaction.Create(
            BankAccountId,
            DateTime.UtcNow,
            "Payment",
            TestAmount,
            TestDirection,
            runningBalance: balance,
            externalTransactionId: "EXT-001",
            importBatchId: batchId);

        transaction.RunningBalance.Should().Be(balance);
        transaction.ExternalTransactionId.Should().Be("EXT-001");
        transaction.ImportBatchId.Should().Be(batchId);
    }

    [Fact]
    public void Create_trims_description()
    {
        var transaction = Transaction.Create(
            BankAccountId, DateTime.UtcNow, "  Groceries  ", TestAmount, TestDirection);

        transaction.Description.Should().Be("Groceries");
    }

    [Fact]
    public void Create_trims_external_transaction_id()
    {
        var transaction = Transaction.Create(
            BankAccountId, DateTime.UtcNow, "Payment", TestAmount, TestDirection,
            externalTransactionId: "  EXT-002  ");

        transaction.ExternalTransactionId.Should().Be("EXT-002");
    }

    [Fact]
    public void Create_raises_TransactionImportedEvent()
    {
        var transaction = Transaction.Create(
            BankAccountId, DateTime.UtcNow, "Payment", TestAmount, TestDirection);

        transaction.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TransactionImportedEvent>()
            .Which.Should().BeEquivalentTo(new
            {
                TransactionId = transaction.Id,
                BankAccountId,
                Amount = 25.50m,
                CurrencyCode = "GBP",
            });
    }

    [Fact]
    public void Create_with_empty_bank_account_id_throws()
    {
        var act = () => Transaction.Create(
            Guid.Empty, DateTime.UtcNow, "Payment", TestAmount, TestDirection);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Create_with_invalid_description_throws(string? description)
    {
        var act = () => Transaction.Create(
            BankAccountId, DateTime.UtcNow, description!, TestAmount, TestDirection);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_with_null_amount_throws()
    {
        var act = () => Transaction.Create(
            BankAccountId, DateTime.UtcNow, "Payment", null!, TestDirection);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_with_null_direction_throws()
    {
        var act = () => Transaction.Create(
            BankAccountId, DateTime.UtcNow, "Payment", TestAmount, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UpdateDescription_changes_description()
    {
        var transaction = Transaction.Create(
            BankAccountId, DateTime.UtcNow, "Old desc", TestAmount, TestDirection);

        transaction.UpdateDescription("New description");

        transaction.Description.Should().Be("New description");
    }

    [Fact]
    public void UpdateDescription_trims_value()
    {
        var transaction = Transaction.Create(
            BankAccountId, DateTime.UtcNow, "Desc", TestAmount, TestDirection);

        transaction.UpdateDescription("  Trimmed  ");

        transaction.Description.Should().Be("Trimmed");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void UpdateDescription_with_invalid_value_throws(string? description)
    {
        var transaction = Transaction.Create(
            BankAccountId, DateTime.UtcNow, "Desc", TestAmount, TestDirection);

        var act = () => transaction.UpdateDescription(description!);

        act.Should().Throw<ArgumentException>();
    }
}
