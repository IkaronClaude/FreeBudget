using FreeBudget.Common.Infrastructure.Middleware;
using FreeBudget.Transactions.Application;
using FreeBudget.Transactions.Application.Commands;
using FreeBudget.Transactions.Application.DTOs;
using FreeBudget.Transactions.Application.Interfaces;
using FreeBudget.Transactions.Application.Queries;
using FreeBudget.Transactions.Domain.Entities;
using FreeBudget.Transactions.Domain.Enums;
using FreeBudget.Transactions.Infrastructure;
using FreeBudget.Transactions.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddTransactionsApplication()
    .AddTransactionsInfrastructure(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TransactionsDbContext>();
    await db.Database.MigrateAsync();

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "Transactions" }));

app.MapPost("/api/transactions/import", async (
    IFormFile file,
    Guid bankAccountId,
    string layout,
    string? currencyMap,
    IMediator mediator,
    IImportLayoutRepository layoutRepository,
    CancellationToken ct) =>
{
    ImportLayout? importLayout;
    if (string.Equals(layout, "saved", StringComparison.OrdinalIgnoreCase))
    {
        var saved = await layoutRepository.GetByBankAccountIdAsync(bankAccountId, ct);
        if (saved is null)
            return Results.BadRequest(new { Error = "No saved import layout for this bank account." });
        importLayout = ToImportLayout(saved);
    }
    else
    {
        importLayout = layout.ToLowerInvariant() switch
        {
            "barclays" => BankLayouts.Barclays(Guid.Empty),
            "wise" => BankLayouts.Wise(Guid.Empty),
            _ => null,
        };
        if (importLayout is null)
            return Results.BadRequest(new { Error = $"Unknown layout: '{layout}'. Supported: barclays, wise, saved" });
    }

    Dictionary<string, Guid>? map = null;
    if (!string.IsNullOrWhiteSpace(currencyMap))
    {
        try
        {
            map = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Guid>>(currencyMap);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { Error = $"Invalid currencyMap: {ex.Message}" });
        }
    }

    await using var stream = file.OpenReadStream();
    var command = new ImportCsvCommand(bankAccountId, stream, importLayout, map);
    var result = await mediator.Send(command, ct);

    if (result.IsFailure)
        return Results.UnprocessableEntity(new { result.Error });

    return Results.Ok(new
    {
        result.Value!.ImportBatchId,
        result.Value.TransactionCount,
        result.Value.SkippedDuplicates,
    });
}).DisableAntiforgery();

app.MapGet("/api/bank-accounts/{id:guid}/import-layout", async (
    Guid id,
    IMediator mediator,
    CancellationToken ct) =>
{
    var layout = await mediator.Send(new GetImportLayoutQuery(id), ct);
    return layout is null ? Results.NotFound() : Results.Ok(layout);
});

app.MapPut("/api/bank-accounts/{id:guid}/import-layout", async (
    Guid id,
    UpsertImportLayoutRequest request,
    IMediator mediator,
    CancellationToken ct) =>
{
    var dto = new ImportLayoutDto(
        null, id, request.Name,
        request.DateColumn, request.DescriptionColumn, request.AmountColumn,
        request.CurrencyColumn, request.DirectionColumn, request.DirectionMappings,
        request.ExternalIdColumn, request.RunningBalanceColumn, request.CategoryColumn,
        request.TargetAmountColumn, request.TargetCurrencyColumn,
        request.DateFormat, request.HasHeaderRow, request.Delimiter, request.DefaultCurrencyCode);
    var result = await mediator.Send(new UpsertImportLayoutCommand(id, request.CreatedByUserId, dto), ct);
    if (result.IsFailure)
        return Results.UnprocessableEntity(new { result.Error });
    return Results.Ok(new { Id = result.Value });
});

app.MapDelete("/api/bank-accounts/{id:guid}/import-layout", async (
    Guid id,
    IMediator mediator,
    CancellationToken ct) =>
{
    var result = await mediator.Send(new DeleteImportLayoutCommand(id), ct);
    if (result.IsFailure)
        return Results.NotFound(new { result.Error });
    return Results.NoContent();
});

app.MapGet("/api/import-layouts/presets", () =>
{
    static ImportLayoutDto Map(ImportLayout l, Guid bankAccountId) => new(
        null, bankAccountId, l.Name,
        l.DateColumn, l.DescriptionColumn, l.AmountColumn,
        l.CurrencyColumn, l.DirectionColumn,
        l.DirectionMappings is null ? null : new Dictionary<string, string>(l.DirectionMappings),
        l.ExternalIdColumn, l.RunningBalanceColumn, l.CategoryColumn,
        l.TargetAmountColumn, l.TargetCurrencyColumn,
        l.DateFormat, l.HasHeaderRow, l.Delimiter.ToString(), l.DefaultCurrencyCode);

    return Results.Ok(new[]
    {
        Map(BankLayouts.Barclays(Guid.Empty), Guid.Empty),
        Map(BankLayouts.Wise(Guid.Empty), Guid.Empty),
    });
});

app.MapGet("/api/sharing-rules", async (
    Guid userId,
    IMediator mediator,
    CancellationToken ct) =>
{
    var rules = await mediator.Send(new GetSharingRulesQuery(userId), ct);
    return Results.Ok(rules);
});

app.MapPost("/api/sharing-rules", async (
    CreateSharingRuleRequest request,
    IMediator mediator,
    CancellationToken ct) =>
{
    if (!Enum.TryParse<RuleMatchType>(request.MatchType, true, out var matchType))
        return Results.BadRequest(new { Error = $"Invalid match type: '{request.MatchType}'." });
    if (!Enum.TryParse<LedgerEntryKind>(request.EntryType ?? "Expense", true, out var entryType))
        return Results.BadRequest(new { Error = $"Invalid entry type: '{request.EntryType}'. Supported: Expense, Settlement" });

    var result = await mediator.Send(new CreateSharingRuleCommand(
        request.UserId, request.Pattern, matchType, entryType, request.Priority,
        request.GroupId, request.PaidByMemberId, request.ParticipantMemberIds), ct);
    if (result.IsFailure)
        return Results.UnprocessableEntity(new { result.Error });
    return Results.Created($"/api/sharing-rules/{result.Value}", new { Id = result.Value });
});

app.MapPut("/api/sharing-rules/{id:guid}", async (
    Guid id,
    UpdateSharingRuleRequest request,
    IMediator mediator,
    CancellationToken ct) =>
{
    if (!Enum.TryParse<RuleMatchType>(request.MatchType, true, out var matchType))
        return Results.BadRequest(new { Error = $"Invalid match type: '{request.MatchType}'." });
    if (!Enum.TryParse<LedgerEntryKind>(request.EntryType ?? "Expense", true, out var entryType))
        return Results.BadRequest(new { Error = $"Invalid entry type: '{request.EntryType}'." });

    var result = await mediator.Send(new UpdateSharingRuleCommand(
        id, request.Pattern, matchType, entryType, request.Priority,
        request.GroupId, request.PaidByMemberId, request.ParticipantMemberIds), ct);
    if (result.IsFailure)
        return Results.NotFound(new { result.Error });
    return Results.NoContent();
});

app.MapDelete("/api/sharing-rules/{id:guid}", async (
    Guid id,
    IMediator mediator,
    CancellationToken ct) =>
{
    var result = await mediator.Send(new DeleteSharingRuleCommand(id), ct);
    if (result.IsFailure)
        return Results.NotFound(new { result.Error });
    return Results.NoContent();
});

app.MapPost("/api/transactions/by-ids", async (
    TransactionsByIdsRequest request,
    IMediator mediator,
    CancellationToken ct) =>
{
    var items = await mediator.Send(new GetTransactionsByIdsQuery(request.Ids ?? []), ct);
    return Results.Ok(items);
});

app.MapPost("/api/transactions/match-transfers", async (
    MatchTransfersRequest request,
    IMediator mediator,
    CancellationToken ct) =>
{
    var result = await mediator.Send(
        new MatchTransfersCommand(request.BankAccountIds, request.RestrictToTransactionIds, request.DateToleranceDays ?? 1), ct);
    if (result.IsFailure)
        return Results.UnprocessableEntity(new { result.Error });
    return Results.Ok(result.Value);
});

app.MapPost("/api/categorization-rules/apply", async (
    ApplyRulesRequest request,
    IMediator mediator,
    CancellationToken ct) =>
{
    var result = await mediator.Send(
        new ApplyRulesToTransactionsCommand(request.UserId, request.BankAccountIds), ct);
    if (result.IsFailure)
        return Results.UnprocessableEntity(new { result.Error });
    return Results.Ok(result.Value);
});

app.MapGet("/api/categorization-rules", async (
    Guid userId,
    IMediator mediator,
    CancellationToken ct) =>
{
    var rules = await mediator.Send(new GetCategorizationRulesQuery(userId), ct);
    return Results.Ok(rules.Select(r => new
    {
        r.Id,
        r.Pattern,
        MatchType = r.RuleMatchType.ToString(),
        r.Category,
        r.Priority,
    }));
});

app.MapPost("/api/categorization-rules", async (
    CreateRuleRequest request,
    IMediator mediator,
    CancellationToken ct) =>
{
    if (!Enum.TryParse<RuleMatchType>(request.MatchType, true, out var matchType))
        return Results.BadRequest(new { Error = $"Invalid match type: '{request.MatchType}'. Supported: Contains, Exact, StartsWith, EndsWith" });

    var command = new CreateCategorizationRuleCommand(
        request.UserId, request.Pattern, matchType, request.Category, request.Priority);
    var result = await mediator.Send(command, ct);

    if (result.IsFailure)
        return Results.UnprocessableEntity(new { result.Error });

    return Results.Created($"/api/categorization-rules/{result.Value}", new { Id = result.Value });
});

app.MapPut("/api/categorization-rules/{id:guid}", async (
    Guid id,
    UpdateRuleRequest request,
    IMediator mediator,
    CancellationToken ct) =>
{
    if (!Enum.TryParse<RuleMatchType>(request.MatchType, true, out var matchType))
        return Results.BadRequest(new { Error = $"Invalid match type: '{request.MatchType}'" });

    var command = new UpdateCategorizationRuleCommand(id, request.Pattern, matchType, request.Category, request.Priority);
    var result = await mediator.Send(command, ct);

    if (result.IsFailure)
        return Results.NotFound(new { result.Error });

    return Results.NoContent();
});

app.MapDelete("/api/categorization-rules/{id:guid}", async (
    Guid id,
    IMediator mediator,
    CancellationToken ct) =>
{
    var result = await mediator.Send(new DeleteCategorizationRuleCommand(id), ct);

    if (result.IsFailure)
        return Results.NotFound(new { result.Error });

    return Results.NoContent();
});

app.MapGet("/api/transactions", async (
    Guid bankAccountId,
    DateTime? from,
    DateTime? to,
    IMediator mediator,
    CancellationToken ct) =>
{
    var result = await mediator.Send(new GetTransactionsByBankAccountQuery(bankAccountId, from, to), ct);
    return Results.Ok(result);
});

app.MapPatch("/api/transactions/{id:guid}/category", async (
    Guid id,
    UpdateCategoryRequest request,
    IMediator mediator,
    CancellationToken ct) =>
{
    var result = await mediator.Send(new UpdateTransactionCategoryCommand(id, request.Category), ct);
    if (result.IsFailure)
        return Results.NotFound(new { result.Error });
    return Results.NoContent();
});

app.MapGet("/api/reports/category-breakdown", async (
    Guid bankAccountId,
    DateTime from,
    DateTime to,
    bool? excludeTransfers,
    IMediator mediator,
    CancellationToken ct) =>
{
    var result = await mediator.Send(
        new GetCategoryBreakdownQuery(bankAccountId, from, to, excludeTransfers ?? true), ct);
    return Results.Ok(result);
});

app.MapGet("/api/reports/period-breakdown", async (
    Guid bankAccountId,
    DateTime from,
    DateTime to,
    string granularity,
    bool? excludeTransfers,
    IMediator mediator,
    CancellationToken ct) =>
{
    if (!Enum.TryParse<PeriodGranularity>(granularity, true, out var g))
        return Results.BadRequest(new { Error = $"Invalid granularity: '{granularity}'. Supported: Day, Week, Month" });

    var result = await mediator.Send(
        new GetPeriodBreakdownQuery(bankAccountId, from, to, g, excludeTransfers ?? true), ct);
    return Results.Ok(result);
});

await app.RunAsync();

static ImportLayout ToImportLayout(ImportLayoutDefinition d) => new()
{
    Name = d.Name,
    BankTypeHint = null,
    DateColumn = d.DateColumn,
    DescriptionColumn = d.DescriptionColumn,
    AmountColumn = d.AmountColumn,
    CurrencyColumn = d.CurrencyColumn,
    DirectionColumn = d.DirectionColumn,
    DirectionMappings = d.DirectionMappings.Count == 0 ? null : d.DirectionMappings,
    ExternalIdColumn = d.ExternalIdColumn,
    RunningBalanceColumn = d.RunningBalanceColumn,
    CategoryColumn = d.CategoryColumn,
    TargetAmountColumn = d.TargetAmountColumn,
    TargetCurrencyColumn = d.TargetCurrencyColumn,
    DateFormat = d.DateFormat,
    HasHeaderRow = d.HasHeaderRow,
    Delimiter = d.Delimiter.Length > 0 ? d.Delimiter[0] : ',',
    DefaultCurrencyCode = d.DefaultCurrencyCode,
    CreatedByUserId = d.CreatedByUserId,
};

record CreateRuleRequest(Guid UserId, string Pattern, string MatchType, string Category, int Priority = 0);
record UpdateRuleRequest(string Pattern, string MatchType, string Category, int Priority);
record UpdateCategoryRequest(string? Category);
record ApplyRulesRequest(Guid UserId, IReadOnlyList<Guid> BankAccountIds);
record CreateSharingRuleRequest(
    Guid UserId, string Pattern, string MatchType, string? EntryType, int Priority,
    Guid GroupId, Guid PaidByMemberId, IReadOnlyList<Guid> ParticipantMemberIds);
record UpdateSharingRuleRequest(
    string Pattern, string MatchType, string? EntryType, int Priority,
    Guid GroupId, Guid PaidByMemberId, IReadOnlyList<Guid> ParticipantMemberIds);
record MatchTransfersRequest(IReadOnlyList<Guid> BankAccountIds, IReadOnlyList<Guid>? RestrictToTransactionIds, int? DateToleranceDays);
record TransactionsByIdsRequest(IReadOnlyList<Guid>? Ids);
record UpsertImportLayoutRequest(
    Guid CreatedByUserId,
    string Name,
    string DateColumn,
    string DescriptionColumn,
    string AmountColumn,
    string? CurrencyColumn,
    string? DirectionColumn,
    Dictionary<string, string>? DirectionMappings,
    string? ExternalIdColumn,
    string? RunningBalanceColumn,
    string? CategoryColumn,
    string? TargetAmountColumn,
    string? TargetCurrencyColumn,
    string DateFormat,
    bool HasHeaderRow,
    string Delimiter,
    string DefaultCurrencyCode);
