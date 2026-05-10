using FreeBudget.Common.Infrastructure.Middleware;
using FreeBudget.Ledger.Application;
using FreeBudget.Ledger.Application.Commands;
using FreeBudget.Ledger.Application.Queries;
using FreeBudget.Ledger.Infrastructure;
using FreeBudget.Ledger.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddLedgerApplication()
    .AddLedgerInfrastructure(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();
    await db.Database.MigrateAsync();

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "Ledger" }));

app.MapPost("/api/ledger/expenses", async (
    CreateExpenseRequest request,
    IMediator mediator,
    CancellationToken ct) =>
{
    var command = new CreateExpenseCommand(
        request.GroupId, request.PaidByUserId, request.OwedByUserId,
        request.Amount, request.CurrencyCode, request.Description,
        request.EntryDate, request.CreatedByUserId, request.TransactionId);
    var result = await mediator.Send(command, ct);

    if (result.IsFailure)
        return Results.UnprocessableEntity(new { result.Error });

    return Results.Created($"/api/ledger/{result.Value}", new { Id = result.Value });
});

app.MapPost("/api/ledger/settlements", async (
    CreateSettlementRequest request,
    IMediator mediator,
    CancellationToken ct) =>
{
    var command = new CreateSettlementCommand(
        request.GroupId, request.PaidByUserId, request.OwedByUserId,
        request.Amount, request.CurrencyCode, request.Description,
        request.EntryDate, request.CreatedByUserId, request.TransactionId);
    var result = await mediator.Send(command, ct);

    if (result.IsFailure)
        return Results.UnprocessableEntity(new { result.Error });

    return Results.Created($"/api/ledger/{result.Value}", new { Id = result.Value });
});

app.MapDelete("/api/ledger/{id:guid}", async (
    Guid id,
    IMediator mediator,
    CancellationToken ct) =>
{
    var result = await mediator.Send(new DeleteLedgerEntryCommand(id), ct);

    if (result.IsFailure)
        return Results.NotFound(new { result.Error });

    return Results.NoContent();
});

app.MapGet("/api/ledger/entries", async (
    Guid groupId,
    IMediator mediator,
    CancellationToken ct) =>
{
    var entries = await mediator.Send(new GetGroupLedgerEntriesQuery(groupId), ct);
    return Results.Ok(entries.Select(e => new
    {
        e.Id,
        e.GroupId,
        e.PaidByUserId,
        e.OwedByUserId,
        Amount = e.Amount.Amount,
        CurrencyCode = e.Amount.CurrencyCode,
        e.Description,
        EntryType = e.EntryType.ToString(),
        e.TransactionId,
        e.EntryDate,
    }));
});

app.MapGet("/api/ledger/balances", async (
    Guid groupId,
    IMediator mediator,
    CancellationToken ct) =>
{
    var balances = await mediator.Send(new GetGroupBalancesQuery(groupId), ct);
    return Results.Ok(balances.Select(b => new
    {
        b.UserId,
        b.OwesToUserId,
        Amount = b.NetAmount.Amount,
        CurrencyCode = b.NetAmount.CurrencyCode,
    }));
});

await app.RunAsync();

record CreateExpenseRequest(
    Guid GroupId, Guid PaidByUserId, Guid OwedByUserId,
    decimal Amount, string CurrencyCode, string Description,
    DateTime EntryDate, Guid CreatedByUserId, Guid? TransactionId = null);

record CreateSettlementRequest(
    Guid GroupId, Guid PaidByUserId, Guid OwedByUserId,
    decimal Amount, string CurrencyCode, string Description,
    DateTime EntryDate, Guid CreatedByUserId, Guid? TransactionId = null);
