using FreeBudget.SharedKernel.Results;
using FreeBudget.Transactions.Application.DTOs;
using MediatR;

namespace FreeBudget.Transactions.Application.Commands;

public sealed record ImportCsvCommand(
    Guid BankAccountId,
    Stream CsvStream,
    ImportLayout Layout) : IRequest<Result<ImportCsvResult>>;

public sealed record ImportCsvResult(
    Guid ImportBatchId,
    int TransactionCount,
    int SkippedDuplicates);
