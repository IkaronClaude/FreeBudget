using FreeBudget.Common.Infrastructure.Middleware;
using FreeBudget.Transactions.Application;
using FreeBudget.Transactions.Application.Commands;
using FreeBudget.Transactions.Application.DTOs;
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

await app.RunAsync();
