using FreeBudget.Web.Api.Clients;
using FreeBudget.Web.Api.CurrentUser;
using FreeBudget.Web.Api.Models;

namespace FreeBudget.Web.Api.Endpoints;

public static class BankAccountsEndpoints
{
    public static IEndpointRouteBuilder MapBankAccountsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/bank-accounts", async (
            CreateBankAccountDto body,
            ICurrentUserResolver currentUser,
            IdentityClient identity,
            CancellationToken ct) =>
        {
            var me = await currentUser.GetAsync(ct);
            var payload = new { OwnerUserId = me.Id, body.BankType, body.Nickname };
            var response = await identity.Http.PostAsJsonAsync("/api/bank-accounts", payload, ct);
            var content = await response.Content.ReadAsStringAsync(ct);
            return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
        });

        app.MapPut("/api/bank-accounts/{id:guid}", async (
            Guid id,
            RenameBankAccountDto body,
            IdentityClient identity,
            CancellationToken ct) =>
        {
            var response = await identity.Http.PutAsJsonAsync($"/api/bank-accounts/{id}", body, ct);
            return response.IsSuccessStatusCode
                ? Results.NoContent()
                : Results.StatusCode((int)response.StatusCode);
        });

        app.MapDelete("/api/bank-accounts/{id:guid}", async (
            Guid id,
            IdentityClient identity,
            CancellationToken ct) =>
        {
            var response = await identity.Http.DeleteAsync($"/api/bank-accounts/{id}", ct);
            return response.IsSuccessStatusCode
                ? Results.NoContent()
                : Results.StatusCode((int)response.StatusCode);
        });

        return app;
    }
}
