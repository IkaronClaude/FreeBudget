using FreeBudget.Web.Api.Clients;

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

        app.MapPost("/api/transactions/import", async (
            HttpRequest request,
            Guid bankAccountId,
            string layout,
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
            var response = await client.Http.PostAsync(url, content, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            return Results.Content(body, response.Content.Headers.ContentType?.MediaType ?? "application/json", statusCode: (int)response.StatusCode);
        }).DisableAntiforgery();

        return app;
    }
}
