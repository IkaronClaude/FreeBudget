using FreeBudget.Web.Api.Clients;
using FreeBudget.Web.Api.CurrentUser;
using FreeBudget.Web.Api.Models;

namespace FreeBudget.Web.Api.Endpoints;

public static class ImportLayoutsEndpoints
{
    public static IEndpointRouteBuilder MapImportLayoutsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/bank-accounts/{id:guid}/import-layout", async (
            Guid id,
            TransactionsClient client,
            CancellationToken ct) =>
        {
            var response = await client.Http.GetAsync($"/api/bank-accounts/{id}/import-layout", ct);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return Results.NotFound();
            var content = await response.Content.ReadAsStringAsync(ct);
            return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
        });

        app.MapPut("/api/bank-accounts/{id:guid}/import-layout", async (
            Guid id,
            UpsertImportLayoutInputDto body,
            ICurrentUserResolver currentUser,
            TransactionsClient client,
            CancellationToken ct) =>
        {
            var me = await currentUser.GetAsync(ct);
            var payload = new
            {
                CreatedByUserId = me.Id,
                body.Name,
                body.DateColumn,
                body.DescriptionColumn,
                body.AmountColumn,
                body.CurrencyColumn,
                body.DirectionColumn,
                body.DirectionMappings,
                body.ExternalIdColumn,
                body.RunningBalanceColumn,
                body.CategoryColumn,
                body.TargetAmountColumn,
                body.TargetCurrencyColumn,
                body.CurrencyAccountMappings,
                body.DateFormat,
                body.HasHeaderRow,
                body.Delimiter,
                body.DefaultCurrencyCode,
            };
            var response = await client.Http.PutAsJsonAsync($"/api/bank-accounts/{id}/import-layout", payload, ct);
            var content = await response.Content.ReadAsStringAsync(ct);
            return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
        });

        app.MapDelete("/api/bank-accounts/{id:guid}/import-layout", async (
            Guid id,
            TransactionsClient client,
            CancellationToken ct) =>
        {
            var response = await client.Http.DeleteAsync($"/api/bank-accounts/{id}/import-layout", ct);
            return response.IsSuccessStatusCode
                ? Results.NoContent()
                : Results.StatusCode((int)response.StatusCode);
        });

        app.MapGet("/api/import-layouts/presets", async (
            TransactionsClient client,
            CancellationToken ct) =>
        {
            var response = await client.Http.GetAsync("/api/import-layouts/presets", ct);
            var content = await response.Content.ReadAsStringAsync(ct);
            return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
        });

        return app;
    }
}
