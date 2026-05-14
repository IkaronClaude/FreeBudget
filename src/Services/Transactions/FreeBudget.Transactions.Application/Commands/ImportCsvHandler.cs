using FreeBudget.SharedKernel.Results;
using FreeBudget.SharedKernel.ValueObjects;
using FreeBudget.Transactions.Application.DTOs;
using FreeBudget.Transactions.Application.Interfaces;
using FreeBudget.Transactions.Domain.Entities;
using FreeBudget.Transactions.Domain.ValueObjects;
using MediatR;

namespace FreeBudget.Transactions.Application.Commands;

internal sealed class ImportCsvHandler(
    ICsvTransactionParser parser,
    ITransactionRepository transactionRepository,
    IImportBatchRepository importBatchRepository,
    ICategorizationRuleRepository ruleRepository,
    ICategorizer categorizer)
    : IRequestHandler<ImportCsvCommand, Result<ImportCsvResult>>
{
    public async Task<Result<ImportCsvResult>> Handle(
        ImportCsvCommand request,
        CancellationToken cancellationToken)
    {
        var batch = ImportBatch.Start(request.BankAccountId);
        await importBatchRepository.AddAsync(batch, cancellationToken);

        try
        {
            var rawTransactions = await parser.ParseAsync(
                request.CsvStream, request.Layout, cancellationToken);

            // Build a case-insensitive currency → bank account map. Default account also
            // covers any unmapped currency.
            var currencyMap = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            if (request.CurrencyToBankAccountMap is not null)
            {
                foreach (var (currency, accountId) in request.CurrencyToBankAccountMap)
                    currencyMap[currency.Trim().ToUpperInvariant()] = accountId;
            }

            Guid ResolveAccount(string currencyCode)
            {
                if (currencyMap.TryGetValue(currencyCode.Trim().ToUpperInvariant(), out var mapped))
                    return mapped;
                return request.BankAccountId;
            }

            var transactions = new List<Transaction>();
            var skipped = 0;

            foreach (var raw in rawTransactions)
            {
                // Zero-amount rows (e.g. Wise's "active card check" refunds) carry no value.
                if (raw.Amount == 0m && (raw.TargetAmount ?? 0m) == 0m)
                    continue;

                // Neutral direction = currency conversion. Emit one debit on the source
                // currency's account plus one credit on the target currency's account.
                if (string.Equals(raw.Direction, "Neutral", StringComparison.OrdinalIgnoreCase))
                {
                    if (!raw.TargetAmount.HasValue || string.IsNullOrWhiteSpace(raw.TargetCurrencyCode))
                    {
                        return Result<ImportCsvResult>.Failure(
                            $"Row with external id '{raw.ExternalTransactionId}' is Neutral direction but is missing target amount/currency.");
                    }

                    var srcAccount = ResolveAccount(raw.CurrencyCode);
                    var tgtAccount = ResolveAccount(raw.TargetCurrencyCode);

                    if (!await AppendIfNew(srcAccount, raw, TransactionDirection.Debit, raw.Amount, raw.CurrencyCode, transactions, batch.Id, cancellationToken))
                        skipped++;
                    if (!await AppendIfNew(tgtAccount, raw, TransactionDirection.Credit, raw.TargetAmount.Value, raw.TargetCurrencyCode, transactions, batch.Id, cancellationToken))
                        skipped++;
                    continue;
                }

                var direction = TransactionDirection.From(raw.Direction);
                var account = ResolveAccount(raw.CurrencyCode);
                if (!await AppendIfNew(account, raw, direction, raw.Amount, raw.CurrencyCode, transactions, batch.Id, cancellationToken))
                    skipped++;
            }

            var rules = await ruleRepository.GetByUserIdAsync(
                request.Layout.CreatedByUserId, cancellationToken);

            foreach (var txn in transactions.Where(t => t.Category is null))
            {
                var category = categorizer.Categorize(txn.Description, rules);
                if (category is not null)
                    txn.UpdateCategory(category);
            }

            if (transactions.Count > 0)
                await transactionRepository.AddRangeAsync(transactions, cancellationToken);

            batch.MarkCompleted(transactions.Count);
            await importBatchRepository.UpdateAsync(batch, cancellationToken);

            return Result<ImportCsvResult>.Success(
                new ImportCsvResult(batch.Id, transactions.Count, skipped));
        }
        catch (Exception ex)
        {
            batch.MarkFailed(ex.Message);
            await importBatchRepository.UpdateAsync(batch, cancellationToken);
            return Result<ImportCsvResult>.Failure(ex.Message);
        }
    }

    private async Task<bool> AppendIfNew(
        Guid bankAccountId,
        RawBankTransaction raw,
        TransactionDirection direction,
        decimal amount,
        string currencyCode,
        List<Transaction> sink,
        Guid batchId,
        CancellationToken ct)
    {
        if (raw.ExternalTransactionId is not null
            && await transactionRepository.ExistsByExternalIdAsync(bankAccountId, raw.ExternalTransactionId, ct))
        {
            return false;
        }
        if (raw.ExternalTransactionId is not null
            && sink.Any(t => t.BankAccountId == bankAccountId && t.ExternalTransactionId == raw.ExternalTransactionId))
        {
            return false;
        }

        var money = new Money(amount, currencyCode);
        var runningBalance = raw.RunningBalance.HasValue
            ? new Money(raw.RunningBalance.Value, currencyCode)
            : null;

        sink.Add(Transaction.Create(
            bankAccountId,
            raw.TransactionDate,
            raw.Description,
            money,
            direction,
            runningBalance,
            raw.ExternalTransactionId,
            batchId,
            raw.Category));
        return true;
    }
}
