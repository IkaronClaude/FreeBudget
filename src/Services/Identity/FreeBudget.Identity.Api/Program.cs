using FreeBudget.Common.Infrastructure.Middleware;
using FreeBudget.Identity.Application;
using FreeBudget.Identity.Application.Commands;
using FreeBudget.Identity.Application.Queries;
using FreeBudget.Identity.Domain.Entities;
using FreeBudget.Identity.Domain.ValueObjects;
using FreeBudget.Identity.Infrastructure;
using FreeBudget.Identity.Infrastructure.Persistence;
using MediatR;
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

    var adminEmail = Email.Create("admin@freebudget.local");
    var admin = await db.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
    if (admin is null)
    {
        admin = User.Create(adminEmail, "Admin");
        await db.Users.AddAsync(admin);
        await db.SaveChangesAsync();
    }

    if (!await db.Groups.AnyAsync(g => g.CreatedByUserId == admin.Id))
    {
        var personalGroup = Group.Create("Personal", admin.Id);
        await db.Groups.AddAsync(personalGroup);
        await db.SaveChangesAsync();
    }

    if (!await db.BankAccounts.AnyAsync(b => b.OwnerUserId == admin.Id))
    {
        var barclaysAccount = BankAccount.Create(admin.Id, BankType.Barclays, "Barclays - Personal");
        await db.BankAccounts.AddAsync(barclaysAccount);
        await db.SaveChangesAsync();
    }

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "Identity" }));

app.MapGet("/api/users", async (
    string email,
    IMediator mediator,
    CancellationToken ct) =>
{
    var user = await mediator.Send(new GetUserByEmailQuery(email), ct);
    return user is null ? Results.NotFound() : Results.Ok(user);
});

app.MapGet("/api/users/{id:guid}/groups", async (
    Guid id,
    IMediator mediator,
    CancellationToken ct) =>
{
    var groups = await mediator.Send(new GetUserGroupsQuery(id), ct);
    return Results.Ok(groups);
});

app.MapGet("/api/users/{id:guid}/bank-accounts", async (
    Guid id,
    IMediator mediator,
    CancellationToken ct) =>
{
    var accounts = await mediator.Send(new GetUserBankAccountsQuery(id), ct);
    return Results.Ok(accounts);
});

app.MapPost("/api/groups", async (
    CreateGroupRequest request,
    IMediator mediator,
    CancellationToken ct) =>
{
    var result = await mediator.Send(
        new CreateGroupCommand(request.Name, request.CreatedByUserId, request.CreatorLabel ?? "me"), ct);
    if (result.IsFailure)
        return Results.UnprocessableEntity(new { result.Error });
    return Results.Created($"/api/groups/{result.Value!.Id}", result.Value);
});

app.MapPut("/api/groups/{id:guid}", async (
    Guid id,
    RenameGroupRequest request,
    IMediator mediator,
    CancellationToken ct) =>
{
    var result = await mediator.Send(new RenameGroupCommand(id, request.Name), ct);
    if (result.IsFailure)
        return Results.NotFound(new { result.Error });
    return Results.NoContent();
});

app.MapDelete("/api/groups/{id:guid}", async (
    Guid id,
    IMediator mediator,
    CancellationToken ct) =>
{
    var result = await mediator.Send(new DeleteGroupCommand(id), ct);
    if (result.IsFailure)
        return Results.NotFound(new { result.Error });
    return Results.NoContent();
});

app.MapPost("/api/groups/{id:guid}/members", async (
    Guid id,
    AddGroupMemberRequest request,
    IMediator mediator,
    CancellationToken ct) =>
{
    var result = await mediator.Send(new AddGroupMemberCommand(id, request.Label, request.OwningUserId), ct);
    if (result.IsFailure)
        return Results.UnprocessableEntity(new { result.Error });
    return Results.Created($"/api/groups/{id}/members/{result.Value!.Id}", result.Value);
});

app.MapPut("/api/groups/{groupId:guid}/members/{memberId:guid}", async (
    Guid groupId,
    Guid memberId,
    RenameGroupMemberRequest request,
    IMediator mediator,
    CancellationToken ct) =>
{
    var result = await mediator.Send(new RenameGroupMemberCommand(groupId, memberId, request.Label), ct);
    if (result.IsFailure)
        return Results.NotFound(new { result.Error });
    return Results.NoContent();
});

app.MapDelete("/api/groups/{groupId:guid}/members/{memberId:guid}", async (
    Guid groupId,
    Guid memberId,
    IMediator mediator,
    CancellationToken ct) =>
{
    var result = await mediator.Send(new RemoveGroupMemberCommand(groupId, memberId), ct);
    if (result.IsFailure)
        return Results.UnprocessableEntity(new { result.Error });
    return Results.NoContent();
});

app.MapPost("/api/bank-accounts", async (
    CreateBankAccountRequest request,
    IMediator mediator,
    CancellationToken ct) =>
{
    var result = await mediator.Send(
        new CreateBankAccountCommand(request.OwnerUserId, request.BankType, request.Nickname), ct);
    if (result.IsFailure)
        return Results.UnprocessableEntity(new { result.Error });
    return Results.Created($"/api/bank-accounts/{result.Value!.Id}", result.Value);
});

app.MapPut("/api/bank-accounts/{id:guid}", async (
    Guid id,
    RenameBankAccountRequest request,
    IMediator mediator,
    CancellationToken ct) =>
{
    var result = await mediator.Send(new RenameBankAccountCommand(id, request.Nickname), ct);
    if (result.IsFailure)
        return Results.NotFound(new { result.Error });
    return Results.NoContent();
});

app.MapDelete("/api/bank-accounts/{id:guid}", async (
    Guid id,
    IMediator mediator,
    CancellationToken ct) =>
{
    var result = await mediator.Send(new DeleteBankAccountCommand(id), ct);
    if (result.IsFailure)
        return Results.NotFound(new { result.Error });
    return Results.NoContent();
});

await app.RunAsync();

record CreateBankAccountRequest(Guid OwnerUserId, string BankType, string Nickname);
record RenameBankAccountRequest(string Nickname);
record CreateGroupRequest(string Name, Guid CreatedByUserId, string? CreatorLabel);
record RenameGroupRequest(string Name);
record AddGroupMemberRequest(string Label, Guid? OwningUserId);
record RenameGroupMemberRequest(string Label);
