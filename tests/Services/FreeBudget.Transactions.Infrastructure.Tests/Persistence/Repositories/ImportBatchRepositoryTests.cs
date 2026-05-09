using FluentAssertions;
using FreeBudget.Transactions.Domain.Entities;
using FreeBudget.Transactions.Domain.Enums;
using FreeBudget.Transactions.Infrastructure.Persistence;
using FreeBudget.Transactions.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FreeBudget.Transactions.Infrastructure.Tests.Persistence.Repositories;

public class ImportBatchRepositoryTests
{
    private static DbContextOptions<TransactionsDbContext> CreateOptions()
        => new DbContextOptionsBuilder<TransactionsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    [Fact]
    public async Task AddAsync_persists_import_batch()
    {
        var options = CreateOptions();
        Guid batchId;

        await using (var context = new TransactionsDbContext(options))
        {
            var repo = new ImportBatchRepository(context);
            var batch = ImportBatch.Start(Guid.NewGuid());
            batchId = batch.Id;
            await repo.AddAsync(batch);
        }

        await using (var context = new TransactionsDbContext(options))
        {
            var found = await context.ImportBatches.FindAsync(batchId);
            found.Should().NotBeNull();
            found!.Status.Should().Be(ImportStatus.InProgress);
        }
    }

    [Fact]
    public async Task GetByIdAsync_returns_batch()
    {
        var options = CreateOptions();
        Guid batchId;

        await using (var context = new TransactionsDbContext(options))
        {
            var batch = ImportBatch.Start(Guid.NewGuid());
            batchId = batch.Id;
            await context.ImportBatches.AddAsync(batch);
            await context.SaveChangesAsync();
        }

        await using (var context = new TransactionsDbContext(options))
        {
            var repo = new ImportBatchRepository(context);
            var found = await repo.GetByIdAsync(batchId);

            found.Should().NotBeNull();
            found!.Id.Should().Be(batchId);
        }
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_when_not_found()
    {
        var options = CreateOptions();
        await using var context = new TransactionsDbContext(options);
        var repo = new ImportBatchRepository(context);

        var found = await repo.GetByIdAsync(Guid.NewGuid());

        found.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_persists_completed_state()
    {
        var options = CreateOptions();
        Guid batchId;

        await using (var context = new TransactionsDbContext(options))
        {
            var batch = ImportBatch.Start(Guid.NewGuid());
            batchId = batch.Id;
            await context.ImportBatches.AddAsync(batch);
            await context.SaveChangesAsync();
        }

        await using (var context = new TransactionsDbContext(options))
        {
            var batch = await context.ImportBatches.FindAsync(batchId);
            batch!.MarkCompleted(42);
            var repo = new ImportBatchRepository(context);
            await repo.UpdateAsync(batch);
        }

        await using (var context = new TransactionsDbContext(options))
        {
            var found = await context.ImportBatches.FindAsync(batchId);
            found!.Status.Should().Be(ImportStatus.Completed);
            found.TransactionCount.Should().Be(42);
            found.CompletedAt.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task UpdateAsync_persists_failed_state()
    {
        var options = CreateOptions();
        Guid batchId;

        await using (var context = new TransactionsDbContext(options))
        {
            var batch = ImportBatch.Start(Guid.NewGuid());
            batchId = batch.Id;
            await context.ImportBatches.AddAsync(batch);
            await context.SaveChangesAsync();
        }

        await using (var context = new TransactionsDbContext(options))
        {
            var batch = await context.ImportBatches.FindAsync(batchId);
            batch!.MarkFailed("Parse error");
            var repo = new ImportBatchRepository(context);
            await repo.UpdateAsync(batch);
        }

        await using (var context = new TransactionsDbContext(options))
        {
            var found = await context.ImportBatches.FindAsync(batchId);
            found!.Status.Should().Be(ImportStatus.Failed);
            found.ErrorMessage.Should().Be("Parse error");
        }
    }
}
