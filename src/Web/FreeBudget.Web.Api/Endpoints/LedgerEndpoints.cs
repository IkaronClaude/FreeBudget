using FreeBudget.Web.Api.Clients;
using FreeBudget.Web.Api.CurrentUser;
using FreeBudget.Web.Api.Models;

namespace FreeBudget.Web.Api.Endpoints;

public static class LedgerEndpoints
{
    public static IEndpointRouteBuilder MapLedgerEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/ledger/splits", async (
            SplitTransactionInputDto body,
            ICurrentUserResolver currentUser,
            LedgerClient ledger,
            CancellationToken ct) =>
        {
            var me = await currentUser.GetAsync(ct);
            var payload = new
            {
                body.GroupId,
                body.PaidByMemberId,
                body.TransactionId,
                body.CurrencyCode,
                body.Description,
                body.EntryDate,
                CreatedByUserId = me.Id,
                body.Participants,
            };
            var response = await ledger.Http.PostAsJsonAsync("/api/ledger/splits", payload, ct);
            var content = await response.Content.ReadAsStringAsync(ct);
            return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
        });

        app.MapPost("/api/ledger/expenses", async (
            CreateLedgerEntryInputDto body,
            ICurrentUserResolver currentUser,
            LedgerClient ledger,
            CancellationToken ct) =>
        {
            var me = await currentUser.GetAsync(ct);
            var payload = MakeEntryPayload(body, me.Id);
            var response = await ledger.Http.PostAsJsonAsync("/api/ledger/expenses", payload, ct);
            var content = await response.Content.ReadAsStringAsync(ct);
            return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
        });

        app.MapPost("/api/ledger/settlements", async (
            CreateLedgerEntryInputDto body,
            ICurrentUserResolver currentUser,
            LedgerClient ledger,
            CancellationToken ct) =>
        {
            var me = await currentUser.GetAsync(ct);
            var payload = MakeEntryPayload(body, me.Id);
            var response = await ledger.Http.PostAsJsonAsync("/api/ledger/settlements", payload, ct);
            var content = await response.Content.ReadAsStringAsync(ct);
            return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
        });

        app.MapGet("/api/ledger/balances", async (
            Guid groupId,
            LedgerClient ledger,
            CancellationToken ct) =>
        {
            var response = await ledger.Http.GetAsync($"/api/ledger/balances?groupId={groupId}", ct);
            var content = await response.Content.ReadAsStringAsync(ct);
            return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
        });

        app.MapGet("/api/ledger/entries", async (
            Guid groupId,
            LedgerClient ledger,
            CancellationToken ct) =>
        {
            var response = await ledger.Http.GetAsync($"/api/ledger/entries?groupId={groupId}", ct);
            var content = await response.Content.ReadAsStringAsync(ct);
            return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
        });

        app.MapDelete("/api/ledger/{id:guid}", async (
            Guid id,
            LedgerClient ledger,
            CancellationToken ct) =>
        {
            var response = await ledger.Http.DeleteAsync($"/api/ledger/{id}", ct);
            return response.IsSuccessStatusCode
                ? Results.NoContent()
                : Results.StatusCode((int)response.StatusCode);
        });

        return app;
    }

    private static object MakeEntryPayload(CreateLedgerEntryInputDto body, Guid createdByUserId) => new
    {
        body.GroupId,
        body.PaidByMemberId,
        body.OwedByMemberId,
        body.Amount,
        body.CurrencyCode,
        body.Description,
        body.EntryDate,
        CreatedByUserId = createdByUserId,
        body.TransactionId,
    };
}
