using FluentAssertions;
using FreeBudget.Transactions.Application.Commands;
using FreeBudget.Transactions.Application.DTOs;
using FreeBudget.Transactions.Application.Interfaces;
using FreeBudget.Transactions.Domain.Entities;
using NSubstitute;

namespace FreeBudget.Transactions.Application.Tests.Commands;

public class ImportCsvHandlerTests
{
    private readonly ICsvTransactionParser _parser = Substitute.For<ICsvTransactionParser>();
    private readonly ITransactionRepository _transactionRepo = Substitute.For<ITransactionRepository>();
    private readonly IImportBatchRepository _importBatchRepo = Substitute.For<IImportBatchRepository>();
    private readonly ImportCsvHandler _handler;

    private static readonly Guid BankAccountId = Guid.NewGuid();

    private static readonly ImportLayout TestLayout = new()
    {
        Name = "Test",
        DateColumn = "Date",
        DescriptionColumn = "Description",
        AmountColumn = "Amount",
        DefaultCurrencyCode = "GBP",
    };

    public ImportCsvHandlerTests()
    {
        _handler = new ImportCsvHandler(_parser, _transactionRepo, _importBatchRepo);
    }

    [Fact]
    public async Task Handle_creates_import_batch_and_transactions()
    {
        var rawTransactions = new List<RawBankTransaction>
        {
            new(null, new DateTime(2024, 5, 1), "PAYMENT ONE", 10.50m, "GBP", "Debit", null),
            new(null, new DateTime(2024, 5, 2), "SALARY", 1500.00m, "GBP", "Credit", null),
        };

        _parser.ParseAsync(Arg.Any<Stream>(), Arg.Any<ImportLayout>(), Arg.Any<CancellationToken>())
            .Returns(rawTransactions);

        var command = new ImportCsvCommand(BankAccountId, Stream.Null, TestLayout);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TransactionCount.Should().Be(2);
        result.Value.SkippedDuplicates.Should().Be(0);

        await _importBatchRepo.Received(1).AddAsync(
            Arg.Any<ImportBatch>(), Arg.Any<CancellationToken>());
        await _transactionRepo.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<Transaction>>(t => t.Count() == 2),
            Arg.Any<CancellationToken>());
        await _importBatchRepo.Received(1).UpdateAsync(
            Arg.Is<ImportBatch>(b => b.Status == Domain.Enums.ImportStatus.Completed),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_skips_duplicate_external_ids()
    {
        var rawTransactions = new List<RawBankTransaction>
        {
            new("EXT-001", new DateTime(2024, 5, 1), "EXISTING", 10.50m, "GBP", "Debit", null),
            new("EXT-002", new DateTime(2024, 5, 2), "NEW ONE", 20.00m, "GBP", "Credit", null),
        };

        _parser.ParseAsync(Arg.Any<Stream>(), Arg.Any<ImportLayout>(), Arg.Any<CancellationToken>())
            .Returns(rawTransactions);

        _transactionRepo.ExistsByExternalIdAsync(BankAccountId, "EXT-001", Arg.Any<CancellationToken>())
            .Returns(true);
        _transactionRepo.ExistsByExternalIdAsync(BankAccountId, "EXT-002", Arg.Any<CancellationToken>())
            .Returns(false);

        var command = new ImportCsvCommand(BankAccountId, Stream.Null, TestLayout);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TransactionCount.Should().Be(1);
        result.Value.SkippedDuplicates.Should().Be(1);
    }

    [Fact]
    public async Task Handle_returns_failure_when_parsing_fails()
    {
        _parser.ParseAsync(Arg.Any<Stream>(), Arg.Any<ImportLayout>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<RawBankTransaction>>(_ => throw new InvalidOperationException("Bad CSV"));

        var command = new ImportCsvCommand(BankAccountId, Stream.Null, TestLayout);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Bad CSV");

        await _importBatchRepo.Received(1).UpdateAsync(
            Arg.Is<ImportBatch>(b => b.Status == Domain.Enums.ImportStatus.Failed),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_handles_empty_csv()
    {
        _parser.ParseAsync(Arg.Any<Stream>(), Arg.Any<ImportLayout>(), Arg.Any<CancellationToken>())
            .Returns(new List<RawBankTransaction>());

        var command = new ImportCsvCommand(BankAccountId, Stream.Null, TestLayout);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TransactionCount.Should().Be(0);

        await _transactionRepo.DidNotReceive().AddRangeAsync(
            Arg.Any<IEnumerable<Transaction>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_creates_transactions_with_running_balance()
    {
        var rawTransactions = new List<RawBankTransaction>
        {
            new(null, new DateTime(2024, 5, 1), "PAYMENT", 10.50m, "GBP", "Debit", 989.50m),
        };

        _parser.ParseAsync(Arg.Any<Stream>(), Arg.Any<ImportLayout>(), Arg.Any<CancellationToken>())
            .Returns(rawTransactions);

        var command = new ImportCsvCommand(BankAccountId, Stream.Null, TestLayout);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _transactionRepo.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<Transaction>>(txns =>
                txns.First().RunningBalance != null &&
                txns.First().RunningBalance!.Amount == 989.50m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_returns_batch_id()
    {
        _parser.ParseAsync(Arg.Any<Stream>(), Arg.Any<ImportLayout>(), Arg.Any<CancellationToken>())
            .Returns(new List<RawBankTransaction>());

        var command = new ImportCsvCommand(BankAccountId, Stream.Null, TestLayout);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.Value!.ImportBatchId.Should().NotBe(Guid.Empty);
    }
}
