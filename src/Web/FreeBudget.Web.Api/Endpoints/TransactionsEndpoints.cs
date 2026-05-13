using FreeBudget.Web.Api.Clients;
using FreeBudget.Web.Api.CurrentUser;
using FreeBudget.Web.Api.Models;

namespace FreeBudget.Web.Api.Endpoints;

public static class TransactionsEndpoints
{
    public static IEndpointRouteBuilder MapTransactionsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/transactions", async (
            Guid bankAccountId,
            DateTime? from,
            DateTime? to,
            TransactionsClient client,
            CancellationToken ct) =>
        {
            var items = await client.ListAsync(bankAccountId, from, to, ct);
            return Results.Ok(items);
        });

        app.MapPost("/api/transactions/match-transfers", async (
            Guid? groupId,
            ICurrentUserResolver currentUser,
            IdentityClient identity,
            TransactionsClient client,
            LedgerClient ledger,
            CancellationToken ct) =>
        {
            var me = await currentUser.GetAsync(ct);
            var accounts = await identity.GetUserBankAccountsAsync(me.Id, ct);

            List<Guid>? restrictToTxnIds = null;
            if (groupId is Guid g)
            {
                var idsResp = await ledger.Http.GetAsync($"/api/ledger/groups/{g}/shared-transaction-ids", ct);
                if (idsResp.IsSuccessStatusCode)
                {
                    restrictToTxnIds = await idsResp.Content.ReadFromJsonAsync<List<Guid>>(cancellationToken: ct) ?? new();
                }
                else
                {
                    return Results.UnprocessableEntity(new { Error = "Failed to load group's shared transactions." });
                }
            }

            var payload = new
            {
                BankAccountIds = accounts.Select(a => a.Id).ToList(),
                RestrictToTransactionIds = restrictToTxnIds,
                DateToleranceDays = (int?)1,
            };
            var response = await client.Http.PostAsJsonAsync("/api/transactions/match-transfers", payload, ct);
            var content = await response.Content.ReadAsStringAsync(ct);
            return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
        });

        app.MapPatch("/api/transactions/{id:guid}/category", async (
            Guid id,
            UpdateCategoryDto body,
            TransactionsClient client,
            CancellationToken ct) =>
        {
            var response = await client.Http.PatchAsJsonAsync($"/api/transactions/{id}/category", body, ct);
            return response.IsSuccessStatusCode
                ? Results.NoContent()
                : Results.StatusCode((int)response.StatusCode);
        });

        app.MapPost("/api/transactions/import", async (
            HttpRequest request,
            Guid bankAccountId,
            string layout,
            string? currencyMap,
            TransactionsClient client,
            CancellationToken ct) =>
        {
            if (!request.HasFormContentType)
                return Results.BadRequest(new { Error = "multipart/form-data required" });

            var form = await request.ReadFormAsync(ct);
            var file = form.Files.GetFile("file");
            if (file is null)
                return Results.BadRequest(new { Error = "Missing 'file' part" });

            using var content = new MultipartFormDataContent();
            using var fileStream = file.OpenReadStream();
            using var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType ?? "text/csv");
            content.Add(streamContent, "file", file.FileName);

            var url = $"/api/transactions/import?bankAccountId={bankAccountId}&layout={Uri.EscapeDataString(layout)}";
            if (!string.IsNullOrWhiteSpace(currencyMap))
                url += $"&currencyMap={Uri.EscapeDataString(currencyMap)}";
            var response = await client.Http.PostAsync(url, content, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            return Results.Content(body, response.Content.Headers.ContentType?.MediaType ?? "application/json", statusCode: (int)response.StatusCode);
        }).DisableAntiforgery();

        return app;
    }
}
