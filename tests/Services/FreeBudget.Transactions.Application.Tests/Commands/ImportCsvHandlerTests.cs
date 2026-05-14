using FluentAssertions;
using FreeBudget.Transactions.Application.Commands;
using FreeBudget.Transactions.Application.DTOs;
using FreeBudget.Transactions.Application.Interfaces;
using FreeBudget.Transactions.Domain.Entities;
using FreeBudget.Transactions.Domain.Enums;
using NSubstitute;

namespace FreeBudget.Transactions.Application.Tests.Commands;

public class ImportCsvHandlerTests
{
    private readonly ICsvTransactionParser _parser = Substitute.For<ICsvTransactionParser>();
    private readonly ITransactionRepository _transactionRepo = Substitute.For<ITransactionRepository>();
    private readonly IImportBatchRepository _importBatchRepo = Substitute.For<IImportBatchRepository>();
    private readonly ICategorizationRuleRepository _ruleRepo = Substitute.For<ICategorizationRuleRepository>();
    private readonly ICategorizer _categorizer = Substitute.For<ICategorizer>();
    private readonly ImportCsvHandler _handler;

    private static readonly Guid BankAccountId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private static readonly ImportLayout TestLayout = new()
    {
        Name = "Test",
        DateColumn = "Date",
        DescriptionColumn = "Description",
        AmountColumn = "Amount",
        DefaultCurrencyCode = "GBP",
        CreatedByUserId = UserId,
    };

    public ImportCsvHandlerTests()
    {
        _ruleRepo.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<CategorizationRule>());
        _handler = new ImportCsvHandler(_parser, _transactionRepo, _importBatchRepo, _ruleRepo, _categorizer);
    }

    [Fact]
    public async Task Handle_creates_import_batch_and_transactions()
    {
        var rawTransactions = new List<RawBankTransaction>
        {
            new(null, new DateTime(2024, 5, 1), "PAYMENT ONE", 10.50m, "GBP", "Debit", null, null),
            new(null, new DateTime(2024, 5, 2), "SALARY", 1500.00m, "GBP", "Credit", null, null),
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
            new("EXT-001", new DateTime(2024, 5, 1), "EXISTING", 10.50m, "GBP", "Debit", null, null),
            new("EXT-002", new DateTime(2024, 5, 2), "NEW ONE", 20.00m, "GBP", "Credit", null, null),
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
            new(null, new DateTime(2024, 5, 1), "PAYMENT", 10.50m, "GBP", "Debit", 989.50m, null),
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

    [Fact]
    public async Task Handle_drops_zero_amount_rows()
    {
        var rawTransactions = new List<RawBankTransaction>
        {
            new("CHECK-1", new DateTime(2024, 5, 1), "ACTIVE CARD CHECK", 0m, "GBP", "Debit", null, null),
            new("REAL-1", new DateTime(2024, 5, 2), "COFFEE", 3.50m, "GBP", "Debit", null, null),
            new("FX-CHECK", new DateTime(2024, 5, 3), "FX CHECK", 0m, "GBP", "Neutral", null, null, 0m, "EUR"),
        };

        _parser.ParseAsync(Arg.Any<Stream>(), Arg.Any<ImportLayout>(), Arg.Any<CancellationToken>())
            .Returns(rawTransactions);

        var command = new ImportCsvCommand(BankAccountId, Stream.Null, TestLayout);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TransactionCount.Should().Be(1);
        result.Value.SkippedDuplicates.Should().Be(0);
        await _transactionRepo.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<Transaction>>(t => t.Count() == 1 && t.First().Description == "COFFEE"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_applies_categorization_rules_to_uncategorized_transactions()
    {
        var rawTransactions = new List<RawBankTransaction>
        {
            new(null, new DateTime(2024, 5, 1), "TESCO STORES", 25.00m, "GBP", "Debit", null, null),
            new(null, new DateTime(2024, 5, 2), "SALARY ACME", 2000.00m, "GBP", "Credit", null, "Income"),
        };

        _parser.ParseAsync(Arg.Any<Stream>(), Arg.Any<ImportLayout>(), Arg.Any<CancellationToken>())
            .Returns(rawTransactions);

        var rules = new List<CategorizationRule>
        {
            CategorizationRule.Create(UserId, "TESCO", RuleMatchType.Contains, "Groceries"),
        };
        _ruleRepo.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(rules);
        _categorizer.Categorize("TESCO STORES", rules).Returns("Groceries");

        var command = new ImportCsvCommand(BankAccountId, Stream.Null, TestLayout);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _transactionRepo.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<Transaction>>(txns =>
                txns.First(t => t.Description == "TESCO STORES").Category == "Groceries" &&
                txns.First(t => t.Description == "SALARY ACME").Category == "Income"),
            Arg.Any<CancellationToken>());
    }
}
