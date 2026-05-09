using FreeBudget.Common.Infrastructure.Middleware;
using FreeBudget.Identity.Application;
using FreeBudget.Identity.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddIdentityApplication()
    .AddIdentityInfrastructure(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "Identity" }));

app.Run();
