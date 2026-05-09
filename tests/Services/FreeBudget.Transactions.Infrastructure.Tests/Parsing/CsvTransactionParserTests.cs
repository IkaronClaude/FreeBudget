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
        var csv = """
            Date,Account,Amount,Subcategory,Memo
            01/05/2024,20-12-34 12345678,-10.50,DD,DIRECT DEBIT COMPANY
            02/05/2024,20-12-34 12345678,1500.00,FPI,SALARY PAYMENT
            """;

        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(csv), BarclaysLayout);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task ParseAsync_parses_date_with_configured_format()
    {
        var csv = """
            Date,Amount,Memo
            15/03/2024,-5.00,TEST
            """;

        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(csv), BarclaysLayout);

        result[0].TransactionDate.Should().Be(new DateTime(2024, 3, 15));
    }

    [Fact]
    public async Task ParseAsync_infers_debit_direction_from_negative_amount()
    {
        var csv = """
            Date,Amount,Memo
            01/05/2024,-10.50,PAYMENT
            """;

        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(csv), BarclaysLayout);

        result[0].Direction.Should().Be("Debit");
        result[0].Amount.Should().Be(10.50m);
    }

    [Fact]
    public async Task ParseAsync_infers_credit_direction_from_positive_amount()
    {
        var csv = """
            Date,Amount,Memo
            01/05/2024,1500.00,SALARY
            """;

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

        var csv = """
            Date,Amount,Description,Type
            2024-05-01,10.50,PAYMENT,Debit
            2024-05-01,1500.00,SALARY,Credit
            """;

        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(csv), layout);

        result[0].Direction.Should().Be("Debit");
        result[0].Amount.Should().Be(10.50m);
        result[1].Direction.Should().Be("Credit");
        result[1].Amount.Should().Be(1500.00m);
    }

    [Fact]
    public async Task ParseAsync_uses_default_currency_when_no_currency_column()
    {
        var csv = """
            Date,Amount,Memo
            01/05/2024,-10.50,PAYMENT
            """;

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
            DateFormat = "yyyy-MM-dd",
            DefaultCurrencyCode = "GBP",
        };

        var csv = """
            Date,Amount,Description,Currency
            2024-05-01,10.50,PAYMENT,EUR
            2024-05-01,20.00,TRANSFER,USD
            """;

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

        var csv = """
            Date,Amount,Description,Balance
            01/05/2024,-10.50,PAYMENT,989.50
            02/05/2024,1500.00,SALARY,2489.50
            """;

        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(csv), layout);

        result[0].RunningBalance.Should().Be(989.50m);
        result[1].RunningBalance.Should().Be(2489.50m);
    }

    [Fact]
    public async Task ParseAsync_running_balance_is_null_when_not_configured()
    {
        var csv = """
            Date,Amount,Memo
            01/05/2024,-10.50,PAYMENT
            """;

        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(csv), BarclaysLayout);

        result[0].RunningBalance.Should().BeNull();
    }

    [Fact]
    public async Task ParseAsync_handles_quoted_fields_with_commas()
    {
        var csv = """
            Date,Amount,Memo
            01/05/2024,-10.50,"PAYMENT, INC  REF123"
            """;

        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(csv), BarclaysLayout);

        result[0].Description.Should().Be("PAYMENT, INC  REF123");
    }

    [Fact]
    public async Task ParseAsync_handles_quoted_fields_with_escaped_quotes()
    {
        var csv = """
            Date,Amount,Memo
            01/05/2024,-10.50,"PAYMENT ""SPECIAL""  REF123"
            """;

        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(csv), BarclaysLayout);

        result[0].Description.Should().Be("PAYMENT \"SPECIAL\"  REF123");
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

        var csv = """
            Date;Amount;Description
            01/05/2024;-10.50;PAYMENT
            """;

        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(csv), layout);

        result.Should().HaveCount(1);
        result[0].Description.Should().Be("PAYMENT");
    }

    [Fact]
    public async Task ParseAsync_skips_empty_rows()
    {
        var csv = "Date,Amount,Memo\r\n01/05/2024,-10.50,PAYMENT\r\n\r\n02/05/2024,20.00,SALARY\r\n";

        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(csv), BarclaysLayout);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task ParseAsync_trims_whitespace_from_fields()
    {
        var csv = """
            Date,Amount,Memo
            01/05/2024 , -10.50 , PAYMENT
            """;

        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(csv), BarclaysLayout);

        result[0].Description.Should().Be("PAYMENT");
        result[0].Amount.Should().Be(10.50m);
    }

    [Fact]
    public async Task ParseAsync_zero_amount_is_credit()
    {
        var csv = """
            Date,Amount,Memo
            01/05/2024,0.00,ZERO TXN
            """;

        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(csv), BarclaysLayout);

        result[0].Direction.Should().Be("Credit");
        result[0].Amount.Should().Be(0.00m);
    }

    [Fact]
    public async Task ParseAsync_returns_null_external_id()
    {
        var csv = """
            Date,Amount,Memo
            01/05/2024,-10.50,PAYMENT
            """;

        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(csv), BarclaysLayout);

        result[0].ExternalTransactionId.Should().BeNull();
    }

    [Fact]
    public async Task ParseAsync_returns_empty_list_for_header_only_csv()
    {
        var csv = """
            Date,Amount,Memo
            """;

        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(csv), BarclaysLayout);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_throws_for_missing_required_column()
    {
        var csv = """
            Date,Wrong,Memo
            01/05/2024,-10.50,PAYMENT
            """;

        var parser = new CsvTransactionParser();

        var act = () => parser.ParseAsync(ToStream(csv), BarclaysLayout);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Amount*");
    }
}
