using FreeBudget.Web.Api.Clients;

namespace FreeBudget.Web.Api.Endpoints;

public static class ReportsEndpoints
{
    public static IEndpointRouteBuilder MapReportsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports/category-breakdown", async (
            Guid bankAccountId,
            DateTime from,
            DateTime to,
            TransactionsClient client,
            CancellationToken ct) =>
        {
            var items = await client.CategoryBreakdownAsync(bankAccountId, from, to, ct);
            return Results.Ok(items);
        });

        app.MapGet("/api/reports/period-breakdown", async (
            Guid bankAccountId,
            DateTime from,
            DateTime to,
            string granularity,
            TransactionsClient client,
            CancellationToken ct) =>
        {
            var items = await client.PeriodBreakdownAsync(bankAccountId, from, to, granularity, ct);
            return Results.Ok(items);
        });

        return app;
    }
}
