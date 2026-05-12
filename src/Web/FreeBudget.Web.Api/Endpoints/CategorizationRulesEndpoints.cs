using FreeBudget.Web.Api.Clients;
using FreeBudget.Web.Api.CurrentUser;
using FreeBudget.Web.Api.Models;

namespace FreeBudget.Web.Api.Endpoints;

public static class CategorizationRulesEndpoints
{
    public static IEndpointRouteBuilder MapCategorizationRulesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/categorization-rules", async (
            ICurrentUserResolver currentUser,
            TransactionsClient client,
            CancellationToken ct) =>
        {
            var me = await currentUser.GetAsync(ct);
            var response = await client.Http.GetAsync($"/api/categorization-rules?userId={me.Id}", ct);
            response.EnsureSuccessStatusCode();
            var rules = await response.Content.ReadFromJsonAsync<List<CategorizationRuleDto>>(cancellationToken: ct) ?? [];
            return Results.Ok(rules);
        });

        app.MapPost("/api/categorization-rules", async (
            CreateCategorizationRuleRequest request,
            ICurrentUserResolver currentUser,
            TransactionsClient client,
            CancellationToken ct) =>
        {
            var me = await currentUser.GetAsync(ct);
            var body = new
            {
                UserId = me.Id,
                request.Pattern,
                request.MatchType,
                request.Category,
                request.Priority,
            };
            var response = await client.Http.PostAsJsonAsync("/api/categorization-rules", body, ct);
            var content = await response.Content.ReadAsStringAsync(ct);
            return Results.Content(content, "application/json", statusCode: (int)response.StatusCode);
        });

        app.MapPut("/api/categorization-rules/{id:guid}", async (
            Guid id,
            UpdateCategorizationRuleRequest request,
            TransactionsClient client,
            CancellationToken ct) =>
        {
            var response = await client.Http.PutAsJsonAsync($"/api/categorization-rules/{id}", request, ct);
            return response.IsSuccessStatusCode
                ? Results.NoContent()
                : Results.StatusCode((int)response.StatusCode);
        });

        app.MapDelete("/api/categorization-rules/{id:guid}", async (
            Guid id,
            TransactionsClient client,
            CancellationToken ct) =>
        {
            var response = await client.Http.DeleteAsync($"/api/categorization-rules/{id}", ct);
            return response.IsSuccessStatusCode
                ? Results.NoContent()
                : Results.StatusCode((int)response.StatusCode);
        });

        return app;
    }
}
