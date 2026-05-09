using FreeBudget.Common.Infrastructure.Middleware;
using FreeBudget.Transactions.Application;
using FreeBudget.Transactions.Application.Commands;
using FreeBudget.Transactions.Application.DTOs;
using FreeBudget.Transactions.Application.Queries;
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
    IMediator mediator,
    CancellationToken ct) =>
{
    var importLayout = layout.ToLowerInvariant() switch
    {
        "barclays" => BankLayouts.Barclays(Guid.Empty),
        "wise" => BankLayouts.Wise(Guid.Empty),
        _ => null,
    };

    if (importLayout is null)
        return Results.BadRequest(new { Error = $"Unknown layout: '{layout}'. Supported: barclays, wise" });

    await using var stream = file.OpenReadStream();
    var command = new ImportCsvCommand(bankAccountId, stream, importLayout);
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

await app.RunAsync();

record CreateRuleRequest(Guid UserId, string Pattern, string MatchType, string Category, int Priority = 0);
record UpdateRuleRequest(string Pattern, string MatchType, string Category, int Priority);
