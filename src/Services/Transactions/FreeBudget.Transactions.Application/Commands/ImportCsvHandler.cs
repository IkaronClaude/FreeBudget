using FreeBudget.SharedKernel.Results;
using FreeBudget.Transactions.Application.Interfaces;
using FreeBudget.Transactions.Domain.Entities;
using FreeBudget.Transactions.Domain.ValueObjects;
using MediatR;

namespace FreeBudget.Transactions.Application.Commands;

internal sealed class ImportCsvHandler(
    ICsvTransactionParser parser,
    ITransactionRepository transactionRepository,
    IImportBatchRepository importBatchRepository)
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

            var transactions = new List<Transaction>();
            var skipped = 0;

            foreach (var raw in rawTransactions)
            {
                if (raw.ExternalTransactionId is not null
                    && await transactionRepository.ExistsByExternalIdAsync(
                        request.BankAccountId, raw.ExternalTransactionId, cancellationToken))
                {
                    skipped++;
                    continue;
                }

                var amount = new Money(raw.Amount, raw.CurrencyCode);
                var direction = TransactionDirection.From(raw.Direction);
                var runningBalance = raw.RunningBalance.HasValue
                    ? new Money(raw.RunningBalance.Value, raw.CurrencyCode)
                    : null;

                transactions.Add(Transaction.Create(
                    request.BankAccountId,
                    raw.TransactionDate,
                    raw.Description,
                    amount,
                    direction,
                    runningBalance,
                    raw.ExternalTransactionId,
                    batch.Id,
                    raw.Category));
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
}
