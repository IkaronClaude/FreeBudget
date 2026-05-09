using FreeBudget.Common.Infrastructure.Middleware;
using FreeBudget.Transactions.Application;
using FreeBudget.Transactions.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddTransactionsApplication()
    .AddTransactionsInfrastructure(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "Transactions" }));

app.Run();
