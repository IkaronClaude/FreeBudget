using FreeBudget.SharedKernel.Results;
using FreeBudget.Transactions.Application.DTOs;
using FreeBudget.Transactions.Application.Interfaces;
using FreeBudget.Transactions.Domain.Entities;
using MediatR;

namespace FreeBudget.Transactions.Application.Commands;

public sealed record UpsertImportLayoutCommand(
    Guid BankAccountId,
    Guid CreatedByUserId,
    ImportLayoutDto Layout) : IRequest<Result<Guid>>;

internal sealed class UpsertImportLayoutHandler(IImportLayoutRepository repository)
    : IRequestHandler<UpsertImportLayoutCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(UpsertImportLayoutCommand request, CancellationToken cancellationToken)
    {
        var existing = await repository.GetByBankAccountIdAsync(request.BankAccountId, cancellationToken);

        try
        {
            if (existing is null)
            {
                var layout = ImportLayoutDefinition.Create(
                    request.BankAccountId,
                    request.CreatedByUserId,
                    request.Layout.Name,
                    request.Layout.DateColumn,
                    request.Layout.DescriptionColumn,
                    request.Layout.AmountColumn,
                    request.Layout.CurrencyColumn,
                    request.Layout.DirectionColumn,
                    request.Layout.DirectionMappings,
                    request.Layout.ExternalIdColumn,
                    request.Layout.RunningBalanceColumn,
                    request.Layout.CategoryColumn,
                    request.Layout.DateFormat,
                    request.Layout.HasHeaderRow,
                    request.Layout.Delimiter,
                    request.Layout.DefaultCurrencyCode);

                await repository.AddAsync(layout, cancellationToken);
                return Result<Guid>.Success(layout.Id);
            }

            existing.Update(
                request.Layout.Name,
                request.Layout.DateColumn,
                request.Layout.DescriptionColumn,
                request.Layout.AmountColumn,
                request.Layout.CurrencyColumn,
                request.Layout.DirectionColumn,
                request.Layout.DirectionMappings,
                request.Layout.ExternalIdColumn,
                request.Layout.RunningBalanceColumn,
                request.Layout.CategoryColumn,
                request.Layout.DateFormat,
                request.Layout.HasHeaderRow,
                request.Layout.Delimiter,
                request.Layout.DefaultCurrencyCode);

            await repository.UpdateAsync(existing, cancellationToken);
            return Result<Guid>.Success(existing.Id);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message);
        }
    }
}
