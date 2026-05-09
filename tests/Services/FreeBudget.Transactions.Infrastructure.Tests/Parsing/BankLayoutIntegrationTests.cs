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

    [Fact]
    public async Task Barclays_layout_parses_typical_export()
    {
        var csv = """
            Number,Date,Account,Amount,Subcategory,Memo
            1,08/05/2024,20-12-34 12345678,-10.50,DD,DIRECT DEBIT CO  REF123
            2,08/05/2024,20-12-34 12345678,-25.99,DEB,CARD PAYMENT  SHOP ABC
            3,09/05/2024,20-12-34 12345678,1500.00,FPI,EMPLOYER LTD  MAY SALARY
            4,09/05/2024,20-12-34 12345678,-5.00,TFR,TRANSFER  TO SAVINGS
            """;

        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(csv), BankLayouts.Barclays(UserId));

        result.Should().HaveCount(4);

        result[0].TransactionDate.Should().Be(new DateTime(2024, 5, 8));
        result[0].Description.Should().Be("DIRECT DEBIT CO  REF123");
        result[0].Amount.Should().Be(10.50m);
        result[0].Direction.Should().Be("Debit");
        result[0].CurrencyCode.Should().Be("GBP");

        result[2].TransactionDate.Should().Be(new DateTime(2024, 5, 9));
        result[2].Description.Should().Be("EMPLOYER LTD  MAY SALARY");
        result[2].Amount.Should().Be(1500.00m);
        result[2].Direction.Should().Be("Credit");
    }

    [Fact]
    public async Task Wise_layout_parses_typical_export()
    {
        var csv = """
            TransferWise ID,Date,Amount,Currency,Description,Payment Reference,Running Balance,Exchange From,Exchange To,Direction
            TRANSFER-12345,08-05-2024,10.50,GBP,Card payment to Shop,REF001,989.50,,,OUT
            TRANSFER-12346,09-05-2024,1500.00,GBP,Transfer from Employer,,2489.50,,,IN
            TRANSFER-12347,09-05-2024,50.00,EUR,Transfer to friend,,200.00,GBP,EUR,OUT
            """;

        var layout = BankLayouts.Wise(UserId);
        var adjustedLayout = layout with { DirectionColumn = "Direction" };

        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(csv), adjustedLayout);

        result.Should().HaveCount(3);

        result[0].TransactionDate.Should().Be(new DateTime(2024, 5, 8));
        result[0].Amount.Should().Be(10.50m);
        result[0].CurrencyCode.Should().Be("GBP");
        result[0].Direction.Should().Be("OUT");

        result[2].CurrencyCode.Should().Be("EUR");
        result[2].Amount.Should().Be(50.00m);
    }

    [Fact]
    public async Task Barclays_layout_handles_memo_with_commas()
    {
        var csv = """
            Number,Date,Account,Amount,Subcategory,Memo
            1,08/05/2024,20-12-34 12345678,-10.50,DD,"COMPANY, INC  PAYMENT REF"
            """;

        var parser = new CsvTransactionParser();
        var result = await parser.ParseAsync(ToStream(csv), BankLayouts.Barclays(UserId));

        result[0].Description.Should().Be("COMPANY, INC  PAYMENT REF");
    }

    [Fact]
    public async Task Barclays_layout_all_fields_are_correct()
    {
        var layout = BankLayouts.Barclays(UserId);

        layout.Name.Should().Be("Barclays");
        layout.BankTypeHint.Should().Be("Barclays");
        layout.DateColumn.Should().Be("Date");
        layout.DescriptionColumn.Should().Be("Memo");
        layout.AmountColumn.Should().Be("Amount");
        layout.DateFormat.Should().Be("dd/MM/yyyy");
        layout.DefaultCurrencyCode.Should().Be("GBP");
        layout.DirectionColumn.Should().BeNull();
        layout.CurrencyColumn.Should().BeNull();
        layout.CreatedByUserId.Should().Be(UserId);
    }

    [Fact]
    public async Task Wise_layout_all_fields_are_correct()
    {
        var layout = BankLayouts.Wise(UserId);

        layout.Name.Should().Be("Wise");
        layout.BankTypeHint.Should().Be("Wise");
        layout.DateColumn.Should().Be("Date");
        layout.DescriptionColumn.Should().Be("Description");
        layout.AmountColumn.Should().Be("Amount");
        layout.CurrencyColumn.Should().Be("Currency");
        layout.DirectionColumn.Should().Be("Direction");
        layout.DateFormat.Should().Be("dd-MM-yyyy");
        layout.DefaultCurrencyCode.Should().Be("GBP");
        layout.CreatedByUserId.Should().Be(UserId);
    }
}
