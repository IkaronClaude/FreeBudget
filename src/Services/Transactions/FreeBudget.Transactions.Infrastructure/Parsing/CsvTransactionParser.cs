using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using FreeBudget.Transactions.Application.DTOs;
using FreeBudget.Transactions.Application.Interfaces;

namespace FreeBudget.Transactions.Infrastructure.Parsing;

internal sealed class CsvTransactionParser : ICsvTransactionParser
{
    public Task<IReadOnlyList<RawBankTransaction>> ParseAsync(
        Stream csvStream,
        ImportLayout layout,
        CancellationToken cancellationToken = default)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = layout.Delimiter.ToString(),
            HasHeaderRecord = layout.HasHeaderRow,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
            ShouldSkipRecord = args => args.Row.Parser.Record?.All(string.IsNullOrWhiteSpace) == true,
        };

        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, config);

        if (layout.HasHeaderRow)
        {
            csv.Read();
            csv.ReadHeader();
            ValidateRequiredColumns(csv, layout);
        }

        var transactions = new List<RawBankTransaction>();

        while (csv.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var dateStr = csv.GetField(layout.DateColumn)!.Trim();
            var date = DateTime.ParseExact(
                dateStr,
                layout.DateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

            var description = csv.GetField(layout.DescriptionColumn)!.Trim();

            var amountStr = csv.GetField(layout.AmountColumn)!.Trim();
            var amountRaw = decimal.Parse(amountStr, NumberStyles.Number, CultureInfo.InvariantCulture);

            string direction;
            decimal amount;

            if (layout.DirectionColumn is not null)
            {
                var rawDirection = csv.GetField(layout.DirectionColumn)!.Trim();
                direction = layout.DirectionMappings is not null
                    && layout.DirectionMappings.TryGetValue(rawDirection, out var mapped)
                        ? mapped
                        : rawDirection;
                amount = Math.Abs(amountRaw);
            }
            else
            {
                direction = amountRaw < 0 ? "Debit" : "Credit";
                amount = Math.Abs(amountRaw);
            }

            var currencyCode = layout.CurrencyColumn is not null
                ? csv.GetField(layout.CurrencyColumn)!.Trim()
                : layout.DefaultCurrencyCode;

            decimal? runningBalance = null;
            if (layout.RunningBalanceColumn is not null)
            {
                var balStr = csv.GetField(layout.RunningBalanceColumn)?.Trim();
                if (!string.IsNullOrEmpty(balStr))
                    runningBalance = decimal.Parse(balStr, NumberStyles.Number, CultureInfo.InvariantCulture);
            }

            string? externalId = null;
            if (layout.ExternalIdColumn is not null)
            {
                var idStr = csv.GetField(layout.ExternalIdColumn)?.Trim();
                if (!string.IsNullOrEmpty(idStr))
                    externalId = idStr;
            }

            string? category = null;
            if (layout.CategoryColumn is not null)
            {
                var catStr = csv.GetField(layout.CategoryColumn)?.Trim();
                if (!string.IsNullOrEmpty(catStr))
                    category = catStr;
            }

            decimal? targetAmount = null;
            if (layout.TargetAmountColumn is not null)
            {
                var tStr = csv.GetField(layout.TargetAmountColumn)?.Trim();
                if (!string.IsNullOrEmpty(tStr))
                    targetAmount = Math.Abs(decimal.Parse(tStr, NumberStyles.Number, CultureInfo.InvariantCulture));
            }

            string? targetCurrency = null;
            if (layout.TargetCurrencyColumn is not null)
            {
                var tcStr = csv.GetField(layout.TargetCurrencyColumn)?.Trim();
                if (!string.IsNullOrEmpty(tcStr))
                    targetCurrency = tcStr;
            }

            transactions.Add(new RawBankTransaction(
                ExternalTransactionId: externalId,
                TransactionDate: date,
                Description: description,
                Amount: amount,
                CurrencyCode: currencyCode,
                Direction: direction,
                RunningBalance: runningBalance,
                Category: category,
                TargetAmount: targetAmount,
                TargetCurrencyCode: targetCurrency));
        }

        return Task.FromResult<IReadOnlyList<RawBankTransaction>>(transactions);
    }

    private static void ValidateRequiredColumns(CsvReader csv, ImportLayout layout)
    {
        var headers = csv.HeaderRecord ?? [];
        var headerSet = new HashSet<string>(headers, StringComparer.OrdinalIgnoreCase);

        var required = new[] { layout.DateColumn, layout.DescriptionColumn, layout.AmountColumn };
        foreach (var col in required)
        {
            if (!headerSet.Contains(col))
                throw new InvalidOperationException(
                    $"Required column '{col}' not found in CSV. Available columns: {string.Join(", ", headers)}");
        }

        // Optional columns are tolerated when absent, but if the layout names a
        // column that the CSV does not actually contain, fail loudly — silently
        // returning null leads to misleading errors deeper in the pipeline
        // (e.g. a Neutral FX row that claims to be missing the target amount).
        var optional = new (string? Column, string Label)[]
        {
            (layout.DirectionColumn, "Direction"),
            (layout.CurrencyColumn, "Currency"),
            (layout.CategoryColumn, "Category"),
            (layout.ExternalIdColumn, "External ID"),
            (layout.RunningBalanceColumn, "Running balance"),
            (layout.TargetAmountColumn, "Target amount"),
            (layout.TargetCurrencyColumn, "Target currency"),
        };
        foreach (var (col, label) in optional)
        {
            if (col is not null && !headerSet.Contains(col))
                throw new InvalidOperationException(
                    $"{label} column '{col}' was configured but not found in CSV. Available columns: {string.Join(", ", headers)}");
        }
    }
}
