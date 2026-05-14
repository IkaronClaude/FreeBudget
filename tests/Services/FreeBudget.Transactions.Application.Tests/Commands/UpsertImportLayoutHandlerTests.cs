using FluentAssertions;
using FreeBudget.Transactions.Application.Commands;
using FreeBudget.Transactions.Application.DTOs;
using FreeBudget.Transactions.Application.Interfaces;
using FreeBudget.Transactions.Domain.Entities;
using NSubstitute;

namespace FreeBudget.Transactions.Application.Tests.Commands;

public class UpsertImportLayoutHandlerTests
{
    private readonly IImportLayoutRepository _repo = Substitute.For<IImportLayoutRepository>();
    private readonly UpsertImportLayoutHandler _handler;
    private static readonly Guid BankAccountId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid GbpAccountId = Guid.NewGuid();
    private static readonly Guid EurAccountId = Guid.NewGuid();

    public UpsertImportLayoutHandlerTests()
    {
        _handler = new UpsertImportLayoutHandler(_repo);
    }

    private static ImportLayoutDto MakeDto(
        string? targetAmount = "Target amount",
        string? targetCurrency = "Target currency",
        Dictionary<string, Guid>? currencyMap = null) =>
        new(
            Id: null,
            BankAccountId: BankAccountId,
            Name: "Wise",
            DateColumn: "Date",
            DescriptionColumn: "Description",
            AmountColumn: "Source amount (after fees)",
            CurrencyColumn: "Source currency",
            DirectionColumn: "Direction",
            DirectionMappings: new Dictionary<string, string> { ["IN"] = "Credit", ["OUT"] = "Debit", ["NEUTRAL"] = "Neutral" },
            ExternalIdColumn: "ID",
            RunningBalanceColumn: null,
            CategoryColumn: null,
            TargetAmountColumn: targetAmount,
            TargetCurrencyColumn: targetCurrency,
            CurrencyAccountMappings: currencyMap,
            DateFormat: "yyyy-MM-dd",
            HasHeaderRow: true,
            Delimiter: ",",
            DefaultCurrencyCode: "GBP");

    [Fact]
    public async Task Creates_layout_with_target_columns_and_currency_account_mappings()
    {
        _repo.GetByBankAccountIdAsync(BankAccountId, Arg.Any<CancellationToken>())
            .Returns((ImportLayoutDefinition?)null);

        var map = new Dictionary<string, Guid> { ["GBP"] = GbpAccountId, ["EUR"] = EurAccountId };
        var dto = MakeDto(currencyMap: map);

        ImportLayoutDefinition? saved = null;
        await _repo.AddAsync(
            Arg.Do<ImportLayoutDefinition>(l => saved = l),
            Arg.Any<CancellationToken>());

        var result = await _handler.Handle(
            new UpsertImportLayoutCommand(BankAccountId, UserId, dto),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        saved.Should().NotBeNull();
        saved!.TargetAmountColumn.Should().Be("Target amount");
        saved.TargetCurrencyColumn.Should().Be("Target currency");
        saved.CurrencyAccountMappings.Should().BeEquivalentTo(
            new Dictionary<string, Guid> { ["GBP"] = GbpAccountId, ["EUR"] = EurAccountId });
    }

    [Fact]
    public async Task Updates_existing_layout_preserving_currency_account_mappings()
    {
        var existing = ImportLayoutDefinition.Create(
            BankAccountId, UserId, "Old", "Date", "Description", "Amount",
            currencyAccountMappings: new Dictionary<string, Guid> { ["USD"] = GbpAccountId });
        _repo.GetByBankAccountIdAsync(BankAccountId, Arg.Any<CancellationToken>())
            .Returns(existing);

        var dto = MakeDto(currencyMap: new Dictionary<string, Guid>
        {
            ["GBP"] = GbpAccountId,
            ["EUR"] = EurAccountId,
        });

        var result = await _handler.Handle(
            new UpsertImportLayoutCommand(BankAccountId, UserId, dto),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existing.TargetAmountColumn.Should().Be("Target amount");
        existing.TargetCurrencyColumn.Should().Be("Target currency");
        existing.CurrencyAccountMappings.Should().BeEquivalentTo(
            new Dictionary<string, Guid> { ["GBP"] = GbpAccountId, ["EUR"] = EurAccountId });
    }

    [Fact]
    public async Task Null_currency_map_persists_as_empty_dictionary()
    {
        _repo.GetByBankAccountIdAsync(BankAccountId, Arg.Any<CancellationToken>())
            .Returns((ImportLayoutDefinition?)null);

        ImportLayoutDefinition? saved = null;
        await _repo.AddAsync(
            Arg.Do<ImportLayoutDefinition>(l => saved = l),
            Arg.Any<CancellationToken>());

        var result = await _handler.Handle(
            new UpsertImportLayoutCommand(BankAccountId, UserId, MakeDto(currencyMap: null)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        saved!.CurrencyAccountMappings.Should().BeEmpty();
    }
}
