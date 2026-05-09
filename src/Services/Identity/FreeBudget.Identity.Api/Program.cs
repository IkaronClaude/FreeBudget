using FreeBudget.Common.Infrastructure.Middleware;
using FreeBudget.Identity.Application;
using FreeBudget.Identity.Domain.Entities;
using FreeBudget.Identity.Domain.ValueObjects;
using FreeBudget.Identity.Infrastructure;
using FreeBudget.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddIdentityApplication()
    .AddIdentityInfrastructure(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    await db.Database.MigrateAsync();

    if (!await db.Users.AnyAsync())
    {
        var adminEmail = Email.Create("admin@freebudget.local");
        var admin = User.Create(adminEmail, "Admin");
        await db.Users.AddAsync(admin);
        await db.SaveChangesAsync();
    }

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "Identity" }));

await app.RunAsync();
