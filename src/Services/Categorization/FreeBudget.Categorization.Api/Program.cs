using FreeBudget.Common.Infrastructure.Middleware;
using FreeBudget.Categorization.Application;
using FreeBudget.Categorization.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddCategorizationApplication()
    .AddCategorizationInfrastructure(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "Categorization" }));

app.Run();
