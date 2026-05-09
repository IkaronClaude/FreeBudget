using FluentAssertions;
using FreeBudget.Transactions.Domain.Entities;
using FreeBudget.Transactions.Domain.Enums;

namespace FreeBudget.Transactions.Domain.Tests.Entities;

public class ImportBatchTests
{
    [Fact]
    public void Start_creates_in_progress_batch()
    {
        var bankAccountId = Guid.NewGuid();

        var batch = ImportBatch.Start(bankAccountId);

        batch.Id.Should().NotBeEmpty();
        batch.BankAccountId.Should().Be(bankAccountId);
        batch.Status.Should().Be(ImportStatus.InProgress);
        batch.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        batch.CompletedAt.Should().BeNull();
        batch.TransactionCount.Should().Be(0);
        batch.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Start_with_empty_bank_account_id_throws()
    {
        var act = () => ImportBatch.Start(Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MarkCompleted_sets_status_and_count()
    {
        var batch = ImportBatch.Start(Guid.NewGuid());

        batch.MarkCompleted(42);

        batch.Status.Should().Be(ImportStatus.Completed);
        batch.TransactionCount.Should().Be(42);
        batch.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkCompleted_with_zero_count_is_valid()
    {
        var batch = ImportBatch.Start(Guid.NewGuid());

        batch.MarkCompleted(0);

        batch.Status.Should().Be(ImportStatus.Completed);
        batch.TransactionCount.Should().Be(0);
    }

    [Fact]
    public void MarkCompleted_with_negative_count_throws()
    {
        var batch = ImportBatch.Start(Guid.NewGuid());

        var act = () => batch.MarkCompleted(-1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void MarkCompleted_when_already_completed_throws()
    {
        var batch = ImportBatch.Start(Guid.NewGuid());
        batch.MarkCompleted(10);

        var act = () => batch.MarkCompleted(5);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkCompleted_when_failed_throws()
    {
        var batch = ImportBatch.Start(Guid.NewGuid());
        batch.MarkFailed("Something broke");

        var act = () => batch.MarkCompleted(5);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkFailed_sets_status_and_error()
    {
        var batch = ImportBatch.Start(Guid.NewGuid());

        batch.MarkFailed("Parse error on row 5");

        batch.Status.Should().Be(ImportStatus.Failed);
        batch.ErrorMessage.Should().Be("Parse error on row 5");
        batch.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkFailed_trims_error_message()
    {
        var batch = ImportBatch.Start(Guid.NewGuid());

        batch.MarkFailed("  Error  ");

        batch.ErrorMessage.Should().Be("Error");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void MarkFailed_with_invalid_error_throws(string? error)
    {
        var batch = ImportBatch.Start(Guid.NewGuid());

        var act = () => batch.MarkFailed(error!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MarkFailed_when_already_completed_throws()
    {
        var batch = ImportBatch.Start(Guid.NewGuid());
        batch.MarkCompleted(10);

        var act = () => batch.MarkFailed("Error");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkFailed_when_already_failed_throws()
    {
        var batch = ImportBatch.Start(Guid.NewGuid());
        batch.MarkFailed("First error");

        var act = () => batch.MarkFailed("Second error");

        act.Should().Throw<InvalidOperationException>();
    }
}
