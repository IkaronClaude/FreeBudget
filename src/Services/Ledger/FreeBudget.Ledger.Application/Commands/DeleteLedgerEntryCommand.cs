using FreeBudget.SharedKernel.Results;
using MediatR;

namespace FreeBudget.Ledger.Application.Commands;

public sealed record DeleteLedgerEntryCommand(Guid EntryId) : IRequest<Result<bool>>;
