using FluentAssertions;
using FreeBudget.Transactions.Domain.Entities;
using FreeBudget.SharedKernel.ValueObjects;
using FreeBudget.Transactions.Domain.ValueObjects;
using FreeBudget.Transactions.Infrastructure.Persistence;
using FreeBudget.Transactions.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FreeBudget.Transactions.Infrastructure.Tests.Persistence.Repositories;

public class TransactionRepositoryTests
{
    private static DbContextOptions<TransactionsDbContext> CreateOptions()
        => new DbContextOptionsBuilder<TransactionsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    [Fact]
    public async Task AddAsync_persists_transaction_with_money()
    {
        var options = CreateOptions();
        Guid txnId;

        await using (var context = new TransactionsDbContext(options))
        {
            var repo = new TransactionRepository(context);
            var txn = Transaction.Create(Guid.NewGuid(), DateTime.UtcNow, "Coffee", new Money(25.50m, "GBP"), TransactionDirection.Debit);
            txnId = txn.Id;
            await repo.AddAsync(txn);
        }

        await using (var context = new TransactionsDbContext(options))
        {
            var found = await context.Transactions.FirstOrDefaultAsync(t => t.Id == txnId);
            found.Should().NotBeNull();
            found!.Description.Should().Be("Coffee");
            found.Amount.Amount.Should().Be(25.50m);
            found.Amount.CurrencyCode.Should().Be("GBP");
        }
    }

    [Fact]
    public async Task GetByIdAsync_returns_transaction()
    {
        var options = CreateOptions();
        Guid txnId;

        await using (var context = new TransactionsDbContext(options))
        {
            var txn = Transaction.Create(Guid.NewGuid(), DateTime.UtcNow, "Groceries", new Money(50m, "GBP"), TransactionDirection.Debit);
            txnId = txn.Id;
            await context.Transactions.AddAsync(txn);
            await context.SaveChangesAsync();
        }

        await using (var context = new TransactionsDbContext(options))
        {
            var repo = new TransactionRepository(context);
            var found = await repo.GetByIdAsync(txnId);

            found.Should().NotBeNull();
            found!.Id.Should().Be(txnId);
        }
    }

    [Fact]
    public async Task GetByBankAccountIdAsync_returns_ordered_by_date_descending()
    {
        var options = CreateOptions();
        var bankAccountId = Guid.NewGuid();

        await using (var context = new TransactionsDbContext(options))
        {
            var older = Transaction.Create(bankAccountId, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), "Older", new Money(10m, "GBP"), TransactionDirection.Debit);
            var newer = Transaction.Create(bankAccountId, new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), "Newer", new Money(20m, "GBP"), TransactionDirection.Credit);
            await context.Transactions.AddRangeAsync(older, newer);
            await context.SaveChangesAsync();
        }

        await using (var context = new TransactionsDbContext(options))
        {
            var repo = new TransactionRepository(context);
            var transactions = await repo.GetByBankAccountIdAsync(bankAccountId);

            transactions.Should().HaveCount(2);
            transactions[0].Description.Should().Be("Newer");
            transactions[1].Description.Should().Be("Older");
        }
    }

    [Fact]
    public async Task ExistsByExternalIdAsync_returns_true_when_exists()
    {
        var options = CreateOptions();
        var bankAccountId = Guid.NewGuid();

        await using (var context = new TransactionsDbContext(options))
        {
            var txn = Transaction.Create(bankAccountId, DateTime.UtcNow, "Payment", new Money(10m, "GBP"), TransactionDirection.Credit, externalTransactionId: "EXT-001");
            await context.Transactions.AddAsync(txn);
            await context.SaveChangesAsync();
        }

        await using (var context = new TransactionsDbContext(options))
        {
            var repo = new TransactionRepository(context);
            var exists = await repo.ExistsByExternalIdAsync(bankAccountId, "EXT-001");

            exists.Should().BeTrue();
        }
    }

    [Fact]
    public async Task ExistsByExternalIdAsync_returns_false_when_not_found()
    {
        var options = CreateOptions();
        await using var context = new TransactionsDbContext(options);
        var repo = new TransactionRepository(context);

        var exists = await repo.ExistsByExternalIdAsync(Guid.NewGuid(), "EXT-999");

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task AddRangeAsync_persists_multiple_transactions()
    {
        var options = CreateOptions();
        var bankAccountId = Guid.NewGuid();

        await using (var context = new TransactionsDbContext(options))
        {
            var repo = new TransactionRepository(context);
            var transactions = new[]
            {
                Transaction.Create(bankAccountId, DateTime.UtcNow, "Txn 1", new Money(10m, "GBP"), TransactionDirection.Debit),
                Transaction.Create(bankAccountId, DateTime.UtcNow, "Txn 2", new Money(20m, "GBP"), TransactionDirection.Credit),
                Transaction.Create(bankAccountId, DateTime.UtcNow, "Txn 3", new Money(30m, "GBP"), TransactionDirection.Debit),
            };
            await repo.AddRangeAsync(transactions);
        }

        await using (var context = new TransactionsDbContext(options))
        {
            var count = await context.Transactions.CountAsync(t => t.BankAccountId == bankAccountId);
            count.Should().Be(3);
        }
    }

    [Fact]
    public async Task GetByBankAccountIdAndDateRangeAsync_filters_by_date_range()
    {
        var options = CreateOptions();
        var bankAccountId = Guid.NewGuid();

        await using (var context = new TransactionsDbContext(options))
        {
            var txns = new[]
            {
                Transaction.Create(bankAccountId, new DateTime(2024, 4, 30), "Before", new Money(10m, "GBP"), TransactionDirection.Debit),
                Transaction.Create(bankAccountId, new DateTime(2024, 5, 1), "Start", new Money(20m, "GBP"), TransactionDirection.Debit),
                Transaction.Create(bankAccountId, new DateTime(2024, 5, 15), "Mid", new Money(30m, "GBP"), TransactionDirection.Credit),
                Transaction.Create(bankAccountId, new DateTime(2024, 5, 31), "End", new Money(40m, "GBP"), TransactionDirection.Debit),
                Transaction.Create(bankAccountId, new DateTime(2024, 6, 1), "After", new Money(50m, "GBP"), TransactionDirection.Debit),
            };
            await context.Transactions.AddRangeAsync(txns);
            await context.SaveChangesAsync();
        }

        await using (var context = new TransactionsDbContext(options))
        {
            var repo = new TransactionRepository(context);
            var results = await repo.GetByBankAccountIdAndDateRangeAsync(
                bankAccountId, new DateTime(2024, 5, 1), new DateTime(2024, 5, 31));

            results.Should().HaveCount(3);
            results[0].Description.Should().Be("Start");
            results[1].Description.Should().Be("Mid");
            results[2].Description.Should().Be("End");
        }
    }

    [Fact]
    public async Task GetByBankAccountIdAndDateRangeAsync_excludes_other_accounts()
    {
        var options = CreateOptions();
        var bankAccountId = Guid.NewGuid();
        var otherAccountId = Guid.NewGuid();

        await using (var context = new TransactionsDbContext(options))
        {
            await context.Transactions.AddRangeAsync(
                Transaction.Create(bankAccountId, new DateTime(2024, 5, 1), "Mine", new Money(10m, "GBP"), TransactionDirection.Debit),
                Transaction.Create(otherAccountId, new DateTime(2024, 5, 1), "Other", new Money(20m, "GBP"), TransactionDirection.Debit));
            await context.SaveChangesAsync();
        }

        await using (var context = new TransactionsDbContext(options))
        {
            var repo = new TransactionRepository(context);
            var results = await repo.GetByBankAccountIdAndDateRangeAsync(
                bankAccountId, new DateTime(2024, 5, 1), new DateTime(2024, 5, 31));

            results.Should().ContainSingle().Which.Description.Should().Be("Mine");
        }
    }

    [Fact]
    public async Task AddAsync_persists_transaction_with_running_balance()
    {
        var options = CreateOptions();
        Guid txnId;

        await using (var context = new TransactionsDbContext(options))
        {
            var repo = new TransactionRepository(context);
            var balance = new Money(500m, "GBP");
            var txn = Transaction.Create(Guid.NewGuid(), DateTime.UtcNow, "With balance", new Money(10m, "GBP"), TransactionDirection.Credit, runningBalance: balance);
            txnId = txn.Id;
            await repo.AddAsync(txn);
        }

        await using (var context = new TransactionsDbContext(options))
        {
            var found = await context.Transactions.FirstOrDefaultAsync(t => t.Id == txnId);
            found!.RunningBalance.Should().NotBeNull();
            found.RunningBalance!.Amount.Should().Be(500m);
            found.RunningBalance.CurrencyCode.Should().Be("GBP");
        }
    }
}
