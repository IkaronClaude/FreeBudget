using System.Text;
using FluentAssertions;
using FreeBudget.Transactions.Application.DTOs;
using FreeBudget.Transactions.Infrastructure.Parsing;

namespace FreeBudget.Transactions.Infrastructure.Tests.Parsing;

public class BankLayoutIntegrationTests
{
    private static Stream ToStream(string csv)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(csv));
    }

    private static readonly Guid UserId = Guid.NewGuid();

    // ── Barclays ────────────────────────────────────────────────────

    private const string BarclaysCsv =
        "Number,Date,Account,Amount,Subcategory,Memo\n" +
        "0,08/05/2026,20-00-00 99887766,-26.35,Standing Order,J SMITH           \tFUNMONEY STO\n" +
        "0,07/05/2026,20-00-00 99887766,-225.05,Card Purchase,TESCO STORES                 \tON 06 MAY BCC\n" +
        "0,07/05/2026,20-00-00 99887766,3.40,Counter Credit,MR J DOE\n" +
        "995416,06/05/2026,20-00-00 99887766,-731.79,Direct Debit,NATIONWIDE          \tSBS00001/123456789 DD\n" +
        "0,05/05/2026,20-00-00 99887766,-5.00,Debit,Blue Rewards Fee CB   \tBlue Rewards Fee CB\n" +
        "0,01/05/2026,20-00-00 99887766,2956.91,Counter Credit,ACME CORP LTD\n" +
        "0,20/04/2026,20-00-00 99887766,-33.98,Card Purchase,AMZNMktplace*NW9OG    \tON 18 APR BCC\n" +
        "0,20/04/2026,20-00-00 99887766,-9.99,Card Purchase,AMAZON UK* NW9O09E    \tON 14 APR BCC\n" +
        ",,,,,\n";

    [Fact]
    public async Task Barclays_parses_standing_order_with_tab_reference()
    {
        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(BarclaysCsv), BankLayouts.Barclays(UserId));

        var so = result[0];
        so.TransactionDate.Should().Be(new DateTime(2026, 5, 8));
        so.Description.Should().Be("J SMITH           \tFUNMONEY STO");
        so.Amount.Should().Be(26.35m);
        so.Direction.Should().Be("Debit");
        so.CurrencyCode.Should().Be("GBP");
    }

    [Fact]
    public async Task Barclays_parses_counter_credit_without_reference()
    {
        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(BarclaysCsv), BankLayouts.Barclays(UserId));

        var credit = result[2];
        credit.Description.Should().Be("MR J DOE");
        credit.Amount.Should().Be(3.40m);
        credit.Direction.Should().Be("Credit");
    }

    [Fact]
    public async Task Barclays_parses_salary_credit()
    {
        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(BarclaysCsv), BankLayouts.Barclays(UserId));

        var salary = result[5];
        salary.Description.Should().Be("ACME CORP LTD");
        salary.Amount.Should().Be(2956.91m);
        salary.Direction.Should().Be("Credit");
    }

    [Fact]
    public async Task Barclays_skips_trailing_empty_row()
    {
        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(BarclaysCsv), BankLayouts.Barclays(UserId));

        result.Should().HaveCount(8);
    }

    [Fact]
    public async Task Barclays_has_no_external_id()
    {
        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(BarclaysCsv), BankLayouts.Barclays(UserId));

        result.Should().AllSatisfy(t => t.ExternalTransactionId.Should().BeNull());
    }

    // ── Wise ────────────────────────────────────────────────────────

    private const string WiseCsv =
        "ID,Status,Direction,Created on,Finished on,Source fee amount,Source fee currency,Target fee amount,Target fee currency,Source name,Source amount (after fees),Source currency,Target name,Target amount (after fees),Target currency,Exchange rate,Reference,Batch,Created by,Category,Note\n" +
        "CARD_TRANSACTION-1234567890,COMPLETED,OUT,08/05/2026 17:36,08/05/2026 17:36,0,GBP,,,J SMITH,4.30,GBP,CORNER SHOP,4.30,GBP,1,,,J SMITH,Shopping,\n" +
        "TRANSFER-9876543210,COMPLETED,IN,08/05/2026 00:13,08/05/2026 00:14,0,GBP,,,JOINT ACCOUNT,33.20,GBP,J SMITH,33.20,GBP,1,MONTHLY TRANSFER,,J SMITH,Money added,\n" +
        "CARD_TRANSACTION-1111222233,COMPLETED,OUT,07/05/2026 18:05,07/05/2026 18:05,0.02,EUR,,,J SMITH,4.97,EUR,BOULANGERIE,4.30,GBP,0.86515,,,J SMITH,Shopping,\n" +
        "DIRECT_DEBIT_TRANSACTION-44455566,COMPLETED,OUT,30/04/2026 07:55,01/05/2026 06:03,0.47,EUR,,,,98.50,EUR,DVLA,84.93,GBP,0.86225,REF-30042026-99999,,J SMITH,General,\n";

    [Fact]
    public async Task Wise_parses_card_transaction()
    {
        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(WiseCsv), BankLayouts.Wise(UserId));

        var card = result[0];
        card.TransactionDate.Should().Be(new DateTime(2026, 5, 8, 17, 36, 0));
        card.Description.Should().Be("CORNER SHOP");
        card.Amount.Should().Be(4.30m);
        card.CurrencyCode.Should().Be("GBP");
        card.Direction.Should().Be("Debit");
        card.ExternalTransactionId.Should().Be("CARD_TRANSACTION-1234567890");
    }

    [Fact]
    public async Task Wise_parses_inbound_transfer()
    {
        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(WiseCsv), BankLayouts.Wise(UserId));

        var transfer = result[1];
        transfer.Direction.Should().Be("Credit");
        transfer.Amount.Should().Be(33.20m);
        transfer.Description.Should().Be("J SMITH");
        transfer.ExternalTransactionId.Should().Be("TRANSFER-9876543210");
    }

    [Fact]
    public async Task Wise_parses_foreign_currency_transaction()
    {
        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(WiseCsv), BankLayouts.Wise(UserId));

        var eur = result[2];
        eur.CurrencyCode.Should().Be("EUR");
        eur.Amount.Should().Be(4.97m);
        eur.Direction.Should().Be("Debit");
    }

    [Fact]
    public async Task Wise_parses_direct_debit()
    {
        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(WiseCsv), BankLayouts.Wise(UserId));

        var dd = result[3];
        dd.Description.Should().Be("DVLA");
        dd.Amount.Should().Be(98.50m);
        dd.CurrencyCode.Should().Be("EUR");
        dd.ExternalTransactionId.Should().Be("DIRECT_DEBIT_TRANSACTION-44455566");
    }

    [Fact]
    public async Task Wise_parses_all_rows()
    {
        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(WiseCsv), BankLayouts.Wise(UserId));

        result.Should().HaveCount(4);
    }

    // ── Layout field assertions ─────────────────────────────────────

    [Fact]
    public void Barclays_layout_has_correct_fields()
    {
        var layout = BankLayouts.Barclays(UserId);

        layout.Name.Should().Be("Barclays");
        layout.DateColumn.Should().Be("Date");
        layout.DescriptionColumn.Should().Be("Memo");
        layout.AmountColumn.Should().Be("Amount");
        layout.DateFormat.Should().Be("dd/MM/yyyy");
        layout.DefaultCurrencyCode.Should().Be("GBP");
        layout.DirectionColumn.Should().BeNull();
        layout.CurrencyColumn.Should().BeNull();
        layout.ExternalIdColumn.Should().BeNull();
    }

    [Fact]
    public void Wise_layout_has_correct_fields()
    {
        var layout = BankLayouts.Wise(UserId);

        layout.Name.Should().Be("Wise");
        layout.DateColumn.Should().Be("Created on");
        layout.DescriptionColumn.Should().Be("Target name");
        layout.AmountColumn.Should().Be("Source amount (after fees)");
        layout.CurrencyColumn.Should().Be("Source currency");
        layout.DirectionColumn.Should().Be("Direction");
        layout.ExternalIdColumn.Should().Be("ID");
        layout.DateFormat.Should().Be("dd/MM/yyyy HH:mm");
        layout.DirectionMappings.Should().ContainKey("IN").WhoseValue.Should().Be("Credit");
        layout.DirectionMappings.Should().ContainKey("OUT").WhoseValue.Should().Be("Debit");
    }
}
