using FreeBudget.Common.Infrastructure.Middleware;
using FreeBudget.Ledger.Application;
using FreeBudget.Ledger.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddLedgerApplication()
    .AddLedgerInfrastructure(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "Ledger" }));

app.Run();
