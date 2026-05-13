using FreeBudget.Transactions.Application.DTOs;
using FreeBudget.Transactions.Application.Interfaces;
using MediatR;

namespace FreeBudget.Transactions.Application.Queries;

public sealed record GetImportLayoutQuery(Guid BankAccountId) : IRequest<ImportLayoutDto?>;

internal sealed class GetImportLayoutHandler(IImportLayoutRepository repository)
    : IRequestHandler<GetImportLayoutQuery, ImportLayoutDto?>
{
    public async Task<ImportLayoutDto?> Handle(GetImportLayoutQuery request, CancellationToken cancellationToken)
    {
        var layout = await repository.GetByBankAccountIdAsync(request.BankAccountId, cancellationToken);
        if (layout is null) return null;

        return new ImportLayoutDto(
            layout.Id,
            layout.BankAccountId,
            layout.Name,
            layout.DateColumn,
            layout.DescriptionColumn,
            layout.AmountColumn,
            layout.CurrencyColumn,
            layout.DirectionColumn,
            layout.DirectionMappings.Count == 0 ? null : new Dictionary<string, string>(layout.DirectionMappings),
            layout.ExternalIdColumn,
            layout.RunningBalanceColumn,
            layout.CategoryColumn,
            layout.TargetAmountColumn,
            layout.TargetCurrencyColumn,
            layout.DateFormat,
            layout.HasHeaderRow,
            layout.Delimiter,
            layout.DefaultCurrencyCode);
    }
}
