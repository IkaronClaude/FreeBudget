using System.Text;
using FluentAssertions;
using FreeBudget.Transactions.Application.DTOs;
using FreeBudget.Transactions.Infrastructure.Parsing;

namespace FreeBudget.Transactions.Infrastructure.Tests.Parsing;

public class CsvTransactionParserTests
{
    private static readonly ImportLayout BarclaysLayout = new()
    {
        Name = "Barclays",
        BankTypeHint = "Barclays",
        DateColumn = "Date",
        DescriptionColumn = "Memo",
        AmountColumn = "Amount",
        DateFormat = "dd/MM/yyyy",
        DefaultCurrencyCode = "GBP",
    };

    private static Stream ToStream(string csv)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(csv));
    }

    [Fact]
    public async Task ParseAsync_parses_basic_rows()
    {
        var csv = "Number,Date,Account,Amount,Subcategory,Memo\n" +
                  "0,15/04/2026,20-00-00 99887766,-42.50,Card Purchase,TESCO STORES\tON 14 APR BCC\n" +
                  "0,14/04/2026,20-00-00 99887766,1800.00,Counter Credit,ACME CORP LTD\n";

        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(csv), BarclaysLayout);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task ParseAsync_parses_date_with_configured_format()
    {
        var csv = "Date,Amount,Memo\n15/03/2026,-5.00,TEST SHOP\n";

        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(csv), BarclaysLayout);

        result[0].TransactionDate.Should().Be(new DateTime(2026, 3, 15));
    }

    [Fact]
    public async Task ParseAsync_infers_debit_direction_from_negative_amount()
    {
        var csv = "Date,Amount,Memo\n01/05/2026,-10.50,CORNER SHOP\n";

        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(csv), BarclaysLayout);

        result[0].Direction.Should().Be("Debit");
        result[0].Amount.Should().Be(10.50m);
    }

    [Fact]
    public async Task ParseAsync_infers_credit_direction_from_positive_amount()
    {
        var csv = "Date,Amount,Memo\n01/05/2026,1500.00,EMPLOYER LTD\n";

        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(csv), BarclaysLayout);

        result[0].Direction.Should().Be("Credit");
        result[0].Amount.Should().Be(1500.00m);
    }

    [Fact]
    public async Task ParseAsync_uses_explicit_direction_column_when_configured()
    {
        var layout = new ImportLayout
        {
            Name = "TestBank",
            DateColumn = "Date",
            DescriptionColumn = "Description",
            AmountColumn = "Amount",
            DirectionColumn = "Type",
            DateFormat = "yyyy-MM-dd",
            DefaultCurrencyCode = "USD",
        };

        var csv = "Date,Amount,Description,Type\n2026-05-01,10.50,PAYMENT,Debit\n2026-05-01,1500.00,SALARY,Credit\n";

        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(csv), layout);

        result[0].Direction.Should().Be("Debit");
        result[0].Amount.Should().Be(10.50m);
        result[1].Direction.Should().Be("Credit");
        result[1].Amount.Should().Be(1500.00m);
    }

    [Fact]
    public async Task ParseAsync_applies_direction_mappings()
    {
        var layout = new ImportLayout
        {
            Name = "Mapped",
            DateColumn = "Date",
            DescriptionColumn = "Description",
            AmountColumn = "Amount",
            DirectionColumn = "Dir",
            DirectionMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["IN"] = "Credit",
                ["OUT"] = "Debit",
            },
            DateFormat = "dd/MM/yyyy HH:mm",
            DefaultCurrencyCode = "GBP",
        };

        var csv = "Date,Amount,Description,Dir\n08/05/2026 17:36,10.50,SHOP,OUT\n08/05/2026 00:13,33.20,TRANSFER IN,IN\n";

        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(csv), layout);

        result[0].Direction.Should().Be("Debit");
        result[1].Direction.Should().Be("Credit");
    }

    [Fact]
    public async Task ParseAsync_uses_default_currency_when_no_currency_column()
    {
        var csv = "Date,Amount,Memo\n01/05/2026,-10.50,SHOP\n";

        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(csv), BarclaysLayout);

        result[0].CurrencyCode.Should().Be("GBP");
    }

    [Fact]
    public async Task ParseAsync_reads_currency_from_column_when_configured()
    {
        var layout = new ImportLayout
        {
            Name = "Multi-currency",
            DateColumn = "Date",
            DescriptionColumn = "Description",
            AmountColumn = "Amount",
            CurrencyColumn = "Currency",
            DateFormat = "dd/MM/yyyy",
            DefaultCurrencyCode = "GBP",
        };

        var csv = "Date,Amount,Description,Currency\n01/05/2026,10.50,PAYMENT,EUR\n01/05/2026,20.00,TRANSFER,USD\n";

        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(csv), layout);

        result[0].CurrencyCode.Should().Be("EUR");
        result[1].CurrencyCode.Should().Be("USD");
    }

    [Fact]
    public async Task ParseAsync_reads_running_balance_when_configured()
    {
        var layout = new ImportLayout
        {
            Name = "WithBalance",
            DateColumn = "Date",
            DescriptionColumn = "Description",
            AmountColumn = "Amount",
            RunningBalanceColumn = "Balance",
            DateFormat = "dd/MM/yyyy",
            DefaultCurrencyCode = "GBP",
        };

        var csv = "Date,Amount,Description,Balance\n01/05/2026,-10.50,PAYMENT,989.50\n02/05/2026,1500.00,SALARY,2489.50\n";

        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(csv), layout);

        result[0].RunningBalance.Should().Be(989.50m);
        result[1].RunningBalance.Should().Be(2489.50m);
    }

    [Fact]
    public async Task ParseAsync_running_balance_is_null_when_not_configured()
    {
        var csv = "Date,Amount,Memo\n01/05/2026,-10.50,PAYMENT\n";

        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(csv), BarclaysLayout);

        result[0].RunningBalance.Should().BeNull();
    }

    [Fact]
    public async Task ParseAsync_reads_external_id_when_configured()
    {
        var layout = new ImportLayout
        {
            Name = "WithId",
            DateColumn = "Date",
            DescriptionColumn = "Description",
            AmountColumn = "Amount",
            ExternalIdColumn = "ID",
            DateFormat = "dd/MM/yyyy",
            DefaultCurrencyCode = "GBP",
        };

        var csv = "ID,Date,Amount,Description\nTXN-123456,01/05/2026,10.50,SHOP\n";

        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(csv), layout);

        result[0].ExternalTransactionId.Should().Be("TXN-123456");
    }

    [Fact]
    public async Task ParseAsync_external_id_is_null_when_not_configured()
    {
        var csv = "Date,Amount,Memo\n01/05/2026,-10.50,SHOP\n";

        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(csv), BarclaysLayout);

        result[0].ExternalTransactionId.Should().BeNull();
    }

    [Fact]
    public async Task ParseAsync_handles_quoted_fields_with_commas()
    {
        var csv = "Date,Amount,Memo\n01/05/2026,-10.50,\"COMPANY, INC\tREF123\"\n";

        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(csv), BarclaysLayout);

        result[0].Description.Should().Be("COMPANY, INC\tREF123");
    }

    [Fact]
    public async Task ParseAsync_uses_configured_delimiter()
    {
        var layout = new ImportLayout
        {
            Name = "Semicolon",
            DateColumn = "Date",
            DescriptionColumn = "Description",
            AmountColumn = "Amount",
            DateFormat = "dd/MM/yyyy",
            DefaultCurrencyCode = "EUR",
            Delimiter = ';',
        };

        var csv = "Date;Amount;Description\n01/05/2026;-10.50;PAYMENT\n";

        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(csv), layout);

        result.Should().HaveCount(1);
        result[0].Description.Should().Be("PAYMENT");
    }

    [Fact]
    public async Task ParseAsync_skips_empty_rows()
    {
        var csv = "Number,Date,Account,Amount,Subcategory,Memo\n" +
                  "0,01/05/2026,20-00-00 99887766,-10.50,Debit,SHOP\n" +
                  ",,,,,\n";

        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(csv), BarclaysLayout);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task ParseAsync_trims_whitespace_from_fields()
    {
        var csv = "Date,Amount,Memo\n 01/05/2026 , -10.50 , SHOP \n";

        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(csv), BarclaysLayout);

        result[0].Description.Should().Be("SHOP");
        result[0].Amount.Should().Be(10.50m);
    }

    [Fact]
    public async Task ParseAsync_zero_amount_is_credit()
    {
        var csv = "Date,Amount,Memo\n01/05/2026,0.00,ZERO TXN\n";

        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(csv), BarclaysLayout);

        result[0].Direction.Should().Be("Credit");
        result[0].Amount.Should().Be(0.00m);
    }

    [Fact]
    public async Task ParseAsync_returns_empty_list_for_header_only_csv()
    {
        var csv = "Date,Amount,Memo\n";

        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(csv), BarclaysLayout);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_throws_for_missing_required_column()
    {
        var csv = "Date,Wrong,Memo\n01/05/2026,-10.50,SHOP\n";

        var parser = new CsvTransactionParser();

        var act = () => parser.ParseAsync(ToStream(csv), BarclaysLayout);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Amount*");
    }

    [Fact]
    public async Task ParseAsync_parses_datetime_with_time_component()
    {
        var layout = new ImportLayout
        {
            Name = "WithTime",
            DateColumn = "Created on",
            DescriptionColumn = "Target name",
            AmountColumn = "Amount",
            DateFormat = "dd/MM/yyyy HH:mm",
            DefaultCurrencyCode = "GBP",
        };

        var csv = "Created on,Amount,Target name\n08/05/2026 17:36,4.30,CORNER SHOP\n";

        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(csv), layout);

        result[0].TransactionDate.Should().Be(new DateTime(2026, 5, 8, 17, 36, 0));
    }
}
